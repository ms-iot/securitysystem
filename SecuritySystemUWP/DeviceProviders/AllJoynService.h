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
#include "IService.h"
#include "IBusObject.h"
#include "IAboutData.h"
#include "AllJoynProvider.h"

namespace DeviceProviders
{
    ref class AllJoynBusObject;
    ref class AllJoynAboutData;

    ref class AllJoynService : public IService
    {
        DEBUG_LIFETIME_DECL(AllJoynService);

    internal:
        AllJoynService(AllJoynProvider ^ provider, std::string serviceName, alljoyn_sessionport port);

        // Note that it is possible to add a BusObject to a service after the initial About broadcast,
        // so this function may be called multiple times with new about info.
        void Initialize(alljoyn_msgarg aboutDataArg, alljoyn_msgarg objectDescriptionArg);
        void Shutdown();
        AllJoynBusObject ^ GetBusObject(const std::string& path);

        inline AllJoynProvider ^ GetProvider() const { return m_provider.Resolve<AllJoynProvider>(); }
        inline alljoyn_busattachment GetBusAttachment() const { return this->GetProvider()->GetBusAttachment(); }
        inline const std::string& GetName() const { return m_name; }
        inline uint32 GetSessionId() const { return m_sessionId; }

    public:
        virtual ~AllJoynService();

        virtual property Windows::Foundation::Collections::IObservableVector<IBusObject ^>^ Objects
        {
            Windows::Foundation::Collections::IObservableVector<IBusObject ^>^ get();
        }
        virtual property Platform::String ^ Name
        {
            Platform::String ^ get();
        }
        virtual property uint16 SessionPort
        {
            uint16 get();
        }
        virtual property uint32 SessionId
        {
            uint32 get();
        }

        virtual property IAboutData^ AboutData
        {
            IAboutData^ get();
        }

    private:
        static void AJ_CALL LoseSession(const void* context, alljoyn_sessionid sessionId, alljoyn_sessionlostreason reason);
        void ParseObjectData();

        std::map<std::string, Platform::WeakReference> m_objectsMap;
        CSLock m_objectsLock;

        Platform::WeakReference m_provider;
        std::string m_name;
        alljoyn_sessionport m_sessionPort;
        alljoyn_sessionid m_sessionId;      
        alljoyn_sessionlistener m_sessionListener;
        alljoyn_msgarg m_aboutDataArg;
        alljoyn_msgarg m_objectDescriptionArg;
    };
}