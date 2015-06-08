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
#include "IProperty.h"

namespace DeviceProviders
{
    ref class AllJoynInterface;
    interface class ITypeDefinition;

    ref class AllJoynProperty : public IProperty
    {
        DEBUG_LIFETIME_DECL(AllJoynProperty);

    internal:
        AllJoynProperty(_In_ AllJoynInterface ^ parent, const alljoyn_interfacedescription_property& propertyDescription);
        AllJoynInterface ^ GetParent() const { return m_interface.Resolve<AllJoynInterface>(); }

        static void AJ_CALL OnPropertyChanged(_In_ alljoyn_proxybusobject busObject,
            _In_ const char* interfaceName,
            _In_ alljoyn_msgarg changed,
            _In_ alljoyn_msgarg invalidated,
            _In_ void *context);

    public:
        virtual ~AllJoynProperty();

        virtual Windows::Foundation::IAsyncOperation<ReadValueResult ^> ^ ReadValueAsync();
        virtual Windows::Foundation::IAsyncOperation<AllJoynStatus ^> ^ SetValueAsync(Platform::Object ^ newValue);

        virtual property Platform::String ^ Name
        {
            Platform::String ^ get();
        }

        virtual property ITypeDefinition ^ TypeInfo
        {
            inline ITypeDefinition ^ get() { return m_typeInfo; }
        }

        virtual property bool CanRead
        {
            inline bool get() { return m_canRead; }
        }

        virtual property bool CanWrite
        {
            inline bool get() { return m_canWrite; }
        }

        virtual event Windows::Foundation::TypedEventHandler<IProperty^, Object^>^ ValueChanged;

    private:
        ::Platform::WeakReference m_interface;
        ITypeDefinition ^ m_typeInfo;
        std::string m_signature;
        std::string m_name;
        bool m_canRead;
        bool m_canWrite;
    };
}