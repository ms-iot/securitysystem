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
#include "IProvider.h"
#include "WorkItemQueue.h"

namespace DeviceProviders
{
    ref class AllJoynService;

    public ref class AllJoynProvider sealed : public IProvider
    {
        DEBUG_LIFETIME_DECL(AllJoynProvider);

    public:
        AllJoynProvider();

        virtual AllJoynStatus^ Start();
        virtual property Windows::Foundation::Collections::IObservableVector<IService ^>^ Services
        {
            Windows::Foundation::Collections::IObservableVector<IService ^>^ get();
        }
        virtual void Shutdown();

    internal:
        static void AJ_CALL AnnounceDiscovery(_In_ void* context,
            _In_ const char* serviceName,
            _In_ uint16_t version,
            _In_ alljoyn_sessionport port,
            _In_ const alljoyn_msgarg objectDescriptionArg,
            _In_ const alljoyn_msgarg aboutDataArg);

        inline alljoyn_busattachment GetBusAttachment() const { return m_bus; }
        void RemoveSession(AllJoynService^ service);


    private:
        ~AllJoynProvider();

        // about service related
        alljoyn_aboutlistener m_aboutListener;
        bool m_isRegistered;
        bool m_isListening;
        WorkItemQueue m_aboutHandlerQueue;

        // main alljoyn objects
        bool m_alljoynInitialized;
        alljoyn_busattachment m_bus;

        Platform::Collections::Vector<IService ^>^ m_servicesVector;
        std::map<std::string, AllJoynService ^> m_servicesMap;
    };
}