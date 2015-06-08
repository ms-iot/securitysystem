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

#pragma once

#include "pch.h"
#include "IBusObject.h"
#include "AllJoynService.h"

namespace DeviceProviders
{
    ref class AllJoynBusObject : public IBusObject
    {
        DEBUG_LIFETIME_DECL(AllJoynBusObject);

    internal:
        AllJoynBusObject(AllJoynService ^ service, const std::string& path, const char **interfaceNames, size_t interfaceCount);
        AllJoynBusObject(AllJoynService ^ service, const std::string& path, alljoyn_proxybusobject proxyBusObject);

        inline const std::string& GetPath() const { return m_path; }
        inline AllJoynService^ GetService() const { return m_service.Resolve<AllJoynService>(); }
        inline alljoyn_proxybusobject GetProxyBusObject() const { return m_proxyBusObject; }
        inline alljoyn_busattachment GetBusAttachment() const { return this->GetService()->GetBusAttachment(); }

    public:
        virtual ~AllJoynBusObject();

        virtual property Windows::Foundation::Collections::IObservableVector<IInterface ^>^ Interfaces
        {
            Windows::Foundation::Collections::IObservableVector<IInterface ^>^ get();
        }
        virtual property Windows::Foundation::Collections::IObservableVector<IBusObject ^>^ ChildObjects
        {
            Windows::Foundation::Collections::IObservableVector<IBusObject ^>^ get();
        }
        virtual property Platform::String ^ Path
        {
            Platform::String ^ get();
        }

    private:
        Platform::WeakReference m_service;
        alljoyn_proxybusobject m_proxyBusObject;
        std::string m_path;      
        bool m_introspectedSuccessfully;
        
        bool AllJoynBusObject::Introspect();

        std::set<std::string> m_interfacesFromAbout;
    };
}