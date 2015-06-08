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
#include "AllJoynBusObject.h"
#include "AllJoynService.h"
#include "AllJoynInterface.h"
#include "AllJoynHelpers.h"

using namespace std;
using namespace Windows::Foundation::Collections;
using namespace Platform::Collections;
using namespace Platform::Details;
using namespace Platform;

namespace DeviceProviders
{

    AllJoynBusObject::AllJoynBusObject(AllJoynService ^ service, const string& path, const char **interfaceNames, size_t interfaceCount)
        : m_service(service)
        , m_proxyBusObject(nullptr)
        , m_path(path)
        , m_introspectedSuccessfully(false)
    {
        DEBUG_LIFETIME_IMPL(AllJoynBusObject);

        for (size_t i = 0; i < interfaceCount; ++i)
        {
            m_interfacesFromAbout.insert(interfaceNames[i]);
        }
    }

    AllJoynBusObject::AllJoynBusObject(AllJoynService ^ service, const string& path, alljoyn_proxybusobject proxyBusObject)
        : m_service(service)
        , m_proxyBusObject(proxyBusObject)
        , m_path(path)
        , m_introspectedSuccessfully(false)
    {
        DEBUG_LIFETIME_IMPL(AllJoynBusObject);
    }

    AllJoynBusObject::~AllJoynBusObject()
    {
        m_interfacesFromAbout.clear();

        if (nullptr != m_proxyBusObject)
        {
            alljoyn_proxybusobject_destroy(m_proxyBusObject);
        }
    }

    bool AllJoynBusObject::Introspect()
    {
        if (!m_introspectedSuccessfully)
        {
            auto service = this->GetService();
            if (service != nullptr)
            {
                if (m_proxyBusObject == nullptr)
                {
                    m_proxyBusObject = alljoyn_proxybusobject_create(service->GetBusAttachment(),
                        service->GetName().c_str(),
                        m_path.c_str(),
                        service->GetSessionId());
                }

                if (m_proxyBusObject != nullptr)
                {
                    if (ER_OK == alljoyn_proxybusobject_introspectremoteobject(m_proxyBusObject))
                    {
                        m_introspectedSuccessfully = true;
                    }
                }
            }
        }
        return m_introspectedSuccessfully;
    }

    IObservableVector<IInterface ^>^ AllJoynBusObject::Interfaces::get()
    {
        auto interfaces = ref new Vector<IInterface ^>();

        if (!this->Introspect())
        {
            return interfaces;
        }     

        // First create the interfaces mentioned in the about Announcement
        for (auto& interfaceNameIterator : m_interfacesFromAbout)
        {
            alljoyn_interfacedescription description = alljoyn_proxybusobject_getinterface(m_proxyBusObject, interfaceNameIterator.data());
            if (nullptr != description)
            {
                interfaces->Append(ref new AllJoynInterface(this, description));
            }
        }

        size_t interfaceCount = alljoyn_proxybusobject_getinterfaces(m_proxyBusObject, nullptr, 0);

        if (interfaceCount > 0)
        {
            auto interfaceDescriptions = vector<alljoyn_interfacedescription>(interfaceCount);
            alljoyn_proxybusobject_getinterfaces(m_proxyBusObject, interfaceDescriptions.data(), interfaceCount);

            for (size_t i = 0; i < interfaceCount; ++i)
            {
                // if the interface was already created because it was part of the about announcement, don't create it again
                string interfaceName = alljoyn_interfacedescription_getname(interfaceDescriptions[i]);
                if (m_interfacesFromAbout.find(interfaceName) == m_interfacesFromAbout.end())
                {
                    interfaces->Append(ref new AllJoynInterface(this, interfaceDescriptions[i]));
                }
            }
        }
        return interfaces;
    }

    IObservableVector<IBusObject ^>^ AllJoynBusObject::ChildObjects::get()
    {
        auto childObjects = ref new Vector<IBusObject ^>();

        if (!this->Introspect())
        {
            return childObjects;
        }

        size_t childCount = alljoyn_proxybusobject_getchildren(m_proxyBusObject, nullptr, 0);

        if (childCount > 0)
        {
            auto children = vector<alljoyn_proxybusobject>(childCount);
            alljoyn_proxybusobject_getchildren(m_proxyBusObject, children.data(), childCount);

            for (size_t i = 0; i < childCount; ++i)
            {
                string path = alljoyn_proxybusobject_getpath(children[i]);

                // Check if we have already created this bus object based on the About announcement
                auto service = this->GetService();
                auto busObject = service->GetBusObject(path);
                if (busObject != nullptr)
                {
                    childObjects->Append(busObject);
                }
                else
                {
                    childObjects->Append(ref new AllJoynBusObject(service, path, children[i]));
                }
            }
        }
        return childObjects;
    }

    String ^ AllJoynBusObject::Path::get()
    {
        return AllJoynHelpers::MultibyteToPlatformString(m_path.c_str());
    }
}