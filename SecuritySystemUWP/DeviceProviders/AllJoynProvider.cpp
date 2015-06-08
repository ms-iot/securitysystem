//
// Copyright (c) 2015, Microsoft Corporation
// 
// Permission to use, copy, modify, and/or distribute this software for any 
// purpose with or without fee is hereby granted, provided that the above 
// copyright notice and this permission notice appear in all copies.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES 
// WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
// SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
// WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN 
// ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR
// IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
//

#include "pch.h"
#include "AllJoynProvider.h"
#include "AllJoynService.h"
#include "AllJoynHelpers.h"

using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Platform::Collections;
using namespace concurrency;
using namespace std;

namespace DeviceProviders
{
    AllJoynProvider::AllJoynProvider()
        : m_aboutListener(nullptr)
        , m_isListening(false)
        , m_isRegistered(false)
        , m_bus(nullptr)
        , m_alljoynInitialized(false)
    {
        DEBUG_LIFETIME_IMPL(AllJoynProvider);

        m_servicesVector = ref new Vector<IService ^>();
    }

    AllJoynProvider::~AllJoynProvider()
    {
        this->Shutdown();
    }

    AllJoynStatus^ AllJoynProvider::Start()
    {
        QStatus status = ER_OK;
        alljoyn_aboutlistener_callback callback = { 0 };

		try
		{
			// nothing to do if already started (m_bus not null)
			if (nullptr != m_bus)
			{
				goto leave;
			}

			// initialize AllJoyn
			if (!m_alljoynInitialized)
			{
			    status = alljoyn_init();
			    if (ER_OK != status)
			    {
			        goto leave;
			    }
			    m_alljoynInitialized = true;
			}

			// create bus attachment and connect
			m_bus = alljoyn_busattachment_create(ALLJOYN_APPLICATION_NAME.c_str(), true);
			if (nullptr == m_bus)
			{
				status = ER_OUT_OF_MEMORY;
				goto leave;
			}

			status = alljoyn_busattachment_start(m_bus);
			if (ER_OK != status)
			{
				goto leave;
			}

			status = alljoyn_busattachment_connect(m_bus, NULL);
			if (ER_OK != status)
			{
				goto leave;
			}

			create_task([this]
			{
				m_aboutHandlerQueue.Start();
			});

			// Register About handler
			callback.about_listener_announced = (alljoyn_about_announced_ptr)AllJoynProvider::AnnounceDiscovery;

			m_aboutListener = alljoyn_aboutlistener_create(&callback, reinterpret_cast<IInspectable *>(this));
			if (nullptr == m_aboutListener)
			{
				status = ER_OUT_OF_MEMORY;
				goto leave;
			}

			alljoyn_busattachment_registeraboutlistener(m_bus, m_aboutListener);
			m_isRegistered = true;

			status = alljoyn_busattachment_whoimplements_interfaces(m_bus, NULL, 0);
			if (ER_OK == status)
			{
				m_isListening = true;
			}
		}
		catch (...)
		{
			status = QStatus::ER_FAIL;
		}

    leave:
        if (ER_OK != status)
        {
            this->Shutdown();
        }

        return ref new AllJoynStatus(status);
    }

    IObservableVector<IService ^>^ AllJoynProvider::Services::get()
    {
        return m_servicesVector;
    }

    void AllJoynProvider::Shutdown()
    {
        // Stop the background task which is processing about announcements
        m_aboutHandlerQueue.PostQuit();

        // Unregister for About announcements before deleting bus attachment
        if (m_isListening)
        {
            alljoyn_busattachment_cancelwhoimplements_interfaces(m_bus, NULL, 0);
            m_isListening = false;
        }
        if (NULL != m_aboutListener)
        {
            if (m_isRegistered)
            {
                alljoyn_busattachment_unregisteraboutlistener(m_bus, m_aboutListener);
                m_isRegistered = false;
            }

            alljoyn_aboutlistener_destroy(m_aboutListener);
            m_aboutListener = NULL;
        }

        m_servicesVector->Clear();
        m_servicesMap.clear();

        // stop and delete bus attachment
        if (nullptr != m_bus)
        {
            alljoyn_busattachment_stop(m_bus);
            alljoyn_busattachment_join(m_bus);
            alljoyn_busattachment_destroy(m_bus);

            m_bus = nullptr;
        }

        // Wait for the workitem queue
        m_aboutHandlerQueue.WaitForQuit();

        // shutdown AllJoyn if necessary
        if (m_alljoynInitialized)
        {
            alljoyn_shutdown();
            m_alljoynInitialized = false;
        }
    }

    void AJ_CALL AllJoynProvider::AnnounceDiscovery(_In_ void* context,
        _In_ const char* _serviceName,
        _In_ uint16_t version,
        _In_ alljoyn_sessionport port,
        _In_ const alljoyn_msgarg _objectDescriptionArg,
        _In_ const alljoyn_msgarg _aboutDataArg)
    {
        UNREFERENCED_PARAMETER(version);

        AllJoynProvider ^provider = reinterpret_cast<AllJoynProvider ^>(context);

        auto objectDescriptionArg = alljoyn_msgarg_copy(_objectDescriptionArg);
        auto aboutDataArg = alljoyn_msgarg_copy(_aboutDataArg);
        string serviceName = _serviceName;

        provider->m_aboutHandlerQueue.PostWorkItem([provider, objectDescriptionArg, aboutDataArg, serviceName, port]
        {
            auto iter = provider->m_servicesMap.find(serviceName);
            if (iter != provider->m_servicesMap.end())
            {
                iter->second->Initialize(aboutDataArg, objectDescriptionArg);
            }
            else
            {
                AllJoynService ^ service = ref new AllJoynService(provider, serviceName, port);
                service->Initialize(aboutDataArg, objectDescriptionArg);

                provider->m_servicesMap[serviceName] = service;
                provider->m_servicesVector->Append(service);
            }
        });
    }

    void AllJoynProvider::RemoveSession(AllJoynService ^ service)
    {
        m_aboutHandlerQueue.PostWorkItem([this, service]
        {
            unsigned int index = 0;
            if (m_servicesVector->IndexOf(service, &index))
            {
                m_servicesVector->RemoveAt(index);
            }

            m_servicesMap.erase(AllJoynHelpers::PlatformToMultibyteStandardString(service->Name));

            service->Shutdown();
        });
    }
}