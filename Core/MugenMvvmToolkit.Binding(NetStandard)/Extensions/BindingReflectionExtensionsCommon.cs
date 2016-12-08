﻿#region Copyright

// ****************************************************************************
// <copyright file="BindingReflectionExtensionsCommon.cs">
// Copyright (c) 2012-2016 Vyacheslav Volkov
// </copyright>
// ****************************************************************************
// <author>Vyacheslav Volkov</author>
// <email>vvs0205@outlook.com</email>
// <project>MugenMvvmToolkit</project>
// <web>https://github.com/MugenMvvmToolkit/MugenMvvmToolkit</web>
// <license>
// See license.txt in this solution or http://opensource.org/licenses/MS-PL
// </license>
// ****************************************************************************

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Interfaces.Models;

// ReSharper disable CheckNamespace
#if ANDROID && XAMARIN_FORMS
namespace MugenMvvmToolkit.Xamarin.Forms.Android
#elif ANDROID
namespace MugenMvvmToolkit.Android
#elif XAMARIN_FORMS && TOUCH
namespace MugenMvvmToolkit.Xamarin.Forms.iOS
#elif TOUCH
namespace MugenMvvmToolkit.iOS
#elif WINFORMS
namespace MugenMvvmToolkit.WinForms
#elif WPF
namespace MugenMvvmToolkit.WPF.Binding
#elif WINDOWS_PHONE && XAMARIN_FORMS
namespace MugenMvvmToolkit.Xamarin.Forms.WinPhone
#else
namespace MugenMvvmToolkit.Binding
#endif
// ReSharper restore CheckNamespace
{
    // ReSharper disable once PartialTypeWithSinglePart
    internal static partial class BindingReflectionExtensions//todo convert remove pcl
    {
#if WPF || ANDROID || TOUCH || WINFORMS || WINDOWS_PHONE
        #region Nested types

        private sealed class MultiTypeConverter : TypeConverter
        {
            #region Fields

            private readonly TypeConverter _first;
            private readonly TypeConverter _second;

            #endregion

            #region Constructors

            public MultiTypeConverter(TypeConverter first, TypeConverter second)
            {
                _first = first;
                _second = second;
            }

            #endregion

            #region Overrides of TypeConverter

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return _first.CanConvertFrom(context, sourceType) || _second.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                var type = value.GetType();
                if (_first.CanConvertFrom(context, type))
                    return _first.ConvertFrom(context, culture, value);
                if (_second.CanConvertFrom(context, type))
                    return _second.ConvertFrom(context, culture, value);
                return base.ConvertFrom(context, culture, value);
            }

            #endregion
        }

        #endregion

        #region Fields

        private static readonly Dictionary<MemberInfo, TypeConverter> MemberToTypeConverter = new Dictionary<MemberInfo, TypeConverter>();

        #endregion

#endif
        #region Methods

        internal static object Convert(IBindingMemberInfo member, Type type, object value)
        {
            if (value == null)
                return type.GetDefaultValue();
            if (type.IsInstanceOfType(value))
                return value;
#if NET_STANDARD
            if (type.GetTypeInfo().IsEnum)
#else
            if (type.IsEnum)
#endif
            {
                var s = value as string;
                if (s == null)
                    return Enum.ToObject(type, value);
                return Enum.Parse(type, s, false);
            }
#if WPF || ANDROID || TOUCH || WINFORMS || WINDOWS_PHONE
            var converter = GetTypeConverter(type, member.Member);
            if (converter != null && converter.CanConvertFrom(value.GetType()))
                return converter.ConvertFrom(value);
#endif
#if NET_STANDARD
            if (TypeCodeTable.ContainsKey(value.GetType()))
#else
            if (value is IConvertible)
#endif
                return System.Convert.ChangeType(value, type.GetNonNullableType(), BindingServiceProvider.BindingCultureInfo());
            if (type == typeof(string))
                return value.ToString();
            return value;
        }

#if WPF || ANDROID || TOUCH || WINFORMS || WINDOWS_PHONE
        private static TypeConverter GetTypeConverter(Type type, MemberInfo member)
        {
            MemberInfo key = member ?? type;
            lock (MemberToTypeConverter)
            {
                TypeConverter value;
                if (!MemberToTypeConverter.TryGetValue(key, out value))
                {
                    var memberValue = GetConverter(member);
#if WINDOWS_PHONE
                    value = GetConverter(type);
#else
                    value = TypeDescriptor.GetConverter(type);
#endif
                    if (value != null && memberValue != null)
                        value = new MultiTypeConverter(memberValue, value);
                    else if (value == null)
                        value = memberValue;
                    MemberToTypeConverter[key] = value;
                }
                return value;
            }
        }

        private static TypeConverter GetConverter(MemberInfo member)
        {
            var attribute = member?.GetCustomAttributes(typeof(TypeConverterAttribute), true)
                .OfType<TypeConverterAttribute>()
                .FirstOrDefault();
            if (attribute == null)
                return null;
            var constructor = Type.GetType(attribute.ConverterTypeName, false)?.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Empty.Array<Type>(), null);
            return constructor?.Invoke(Empty.Array<object>()) as TypeConverter;
        }
#endif
        private static Type GetNonNullableType(this Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        private static bool IsNullableType(this Type type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

#if !NET_STANDARD
        internal static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }

        private static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }
#endif
        #endregion
    }
}
