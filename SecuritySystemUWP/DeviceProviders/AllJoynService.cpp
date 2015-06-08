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
#include "AllJoynService.h"
#include "AllJoynBusObject.h"
#include "AllJoynProvider.h"
#include "AllJoynHelpers.h"
#include "AllJoynAboutData.h"

using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Platform::Collections;
using namespace Platform;
using namespace std;

namespace DeviceProviders
{
    AllJoynService::AllJoynService(AllJoynProvider ^ provider, std::string serviceName, alljoyn_sessionport port)
        : m_provider(provider)
        , m_name(serviceName)
        , m_sessionPort(port)
        , m_sessionListener(nullptr)
        , m_aboutDataArg(nullptr)
        , m_objectDescriptionArg(nullptr)
    {
        DEBUG_LIFETIME_IMPL(AllJoynService);

        alljoyn_sessionlistener_callbacks sessionListenerCallbacks = { LoseSession, nullptr, nullptr };
        m_sessionListener = alljoyn_sessionlistener_create(&sessionListenerCallbacks, reinterpret_cast<IInspectable *>(this));

        alljoyn_sessionopts sessionOpts = alljoyn_sessionopts_create(ALLJOYN_TRAFFIC_TYPE_MESSAGES, QCC_FALSE, ALLJOYN_PROXIMITY_ANY, ALLJOYN_TRANSPORT_ANY);

        (void) alljoyn_busattachment_joinsession(this->GetProvider()->GetBusAttachment(),
            m_name.c_str(),
            m_sessionPort,
            m_sessionListener,
            &m_sessionId,
            sessionOpts);
    }

    AllJoynService::~AllJoynService()
    {
        this->Shutdown();
    }

    void AllJoynService::Initialize(alljoyn_msgarg aboutDataArg, alljoyn_msgarg objectDescriptionArg)
    {
        if (m_aboutDataArg != nullptr)
        {
            alljoyn_msgarg_destroy(m_aboutDataArg);
        }
        if (m_objectDescriptionArg != nullptr)
        {
            alljoyn_msgarg_destroy(m_objectDescriptionArg);
        }

        m_aboutDataArg = aboutDataArg;
        m_objectDescriptionArg = objectDescriptionArg;
    }

    void AJ_CALL AllJoynService::LoseSession(const void* context, alljoyn_sessionid sessionId, alljoyn_sessionlostreason reason)
    {
        UNREFERENCED_PARAMETER(reason);

        auto pThis = reinterpret_cast<AllJoynService ^>(const_cast<void*>(context));
        if (sessionId == pThis->SessionId)
        {
            pThis->GetProvider()->RemoveSession(pThis);
        }
    };

    void AllJoynService::ParseObjectData()
    {

    }

    AllJoynBusObject ^ AllJoynService::GetBusObject(const string& path)
    {
        AllJoynBusObject^ busObject = nullptr;

        AutoLock lock(&m_objectsLock, true);
        auto it = m_objectsMap.find(path);
        if (it != m_objectsMap.end())
        {
            busObject = m_objectsMap.at(path).Resolve<AllJoynBusObject>();
            if (busObject == nullptr)
            {
                m_objectsMap.erase(it);
            }
        }
        return busObject;
    }

    IObservableVector<IBusObject^> ^ AllJoynService::Objects::get()
    {
        auto busObjects = ref new Vector<IBusObject^>();

        auto objectDescription = alljoyn_aboutobjectdescription_create_full(m_objectDescriptionArg);
        if (nullptr == objectDescription)
        {
            return busObjects;
        }

        // get number of bus object path
        size_t pathCount = alljoyn_aboutobjectdescription_getpaths(objectDescription, nullptr, 0);
        if (0 != pathCount)
        {
            auto pathArray = vector<const char*>(pathCount);
            alljoyn_aboutobjectdescription_getpaths(objectDescription, pathArray.data(), pathCount);
            
            AutoLock lock(&m_objectsLock, true);
            for (size_t i = 0; i < pathCount; ++i)
            {
                size_t interfaceCount = 0;
                interfaceCount = alljoyn_aboutobjectdescription_getinterfaces(objectDescription, pathArray[i], nullptr, 0);
                vector<const char*> interfacesArray;

                if (0 != interfaceCount)
                {
                    interfacesArray = vector<const char*>(interfaceCount);
                    alljoyn_aboutobjectdescription_getinterfaces(objectDescription, pathArray[i], interfacesArray.data(), interfaceCount);
                }

                auto newObject = ref new AllJoynBusObject(this, pathArray[i], interfacesArray.data(), interfaceCount);
                m_objectsMap.insert(pair<string, WeakReference>(pathArray[i], WeakReference(newObject)));
                busObjects->Append(newObject);
            }
        }

        alljoyn_aboutobjectdescription_destroy(objectDescription);
        return busObjects;        
    }

    String ^ AllJoynService::Name::get()
    {
        return AllJoynHelpers::MultibyteToPlatformString(m_name.c_str());
    }

    uint16 AllJoynService::SessionPort::get()
    {
        return m_sessionPort;
    }

    uint32 AllJoynService::SessionId::get()
    {
        return m_sessionId;
    }

    IAboutData ^ AllJoynService::AboutData::get()
    {
        return ref new AllJoynAboutData(m_aboutDataArg);
    }

    void AllJoynService::Shutdown()
    {
        if (m_aboutDataArg != nullptr)
        {
            alljoyn_msgarg_destroy(m_aboutDataArg);
            m_aboutDataArg = nullptr;
        }

        if (m_objectDescriptionArg != nullptr)
        {
            alljoyn_msgarg_destroy(m_objectDescriptionArg);
            m_objectDescriptionArg = nullptr;
        }

        if (m_sessionId != 0)
        {
            alljoyn_busattachment_leavesession(this->GetProvider()->GetBusAttachment(), m_sessionId);
            m_sessionId = 0;
        }

        if (m_sessionListener != nullptr)
        {
            alljoyn_sessionlistener_destroy(m_sessionListener);
            m_sessionListener = nullptr;
        }

        AutoLock lock(&m_objectsLock, true);
        m_objectsMap.clear();
    }
}