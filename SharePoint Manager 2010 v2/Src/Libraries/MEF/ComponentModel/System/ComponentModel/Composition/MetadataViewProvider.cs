﻿// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Internal;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.ComponentModel.Composition
{
    internal static class MetadataViewProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static TMetadataView GetMetadataView<TMetadataView>(IDictionary<string, object> metadata)
        {
            Type metadataViewType = typeof(TMetadataView);

            // If the Metadata dictionary is cast compatible with the passed in type
            if (metadataViewType.IsAssignableFrom(typeof(IDictionary<string, object>)))
            {
                return (TMetadataView)metadata;
            }
            // otherwise is it a metadata view
            else
            {
                Type proxyType;
                if (metadataViewType.IsInterface)
                {
                    try
                    {
                        proxyType = MetadataViewGenerator.GenerateView(metadataViewType);
                    }
                    catch (TypeLoadException ex)
                    {
                        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Strings.NotSupportedInterfaceMetadataView, metadataViewType.FullName), ex);
                    }
                }
                else
                {
                    proxyType = metadataViewType;
                }

                // Now we have the type for the proxy create it
                try
                {
                    return (TMetadataView)Activator.CreateInstance(proxyType, new object[] { metadata });
                }
                catch(TargetInvocationException ex)
                {
                    //Unwrap known failures that we want to present as CompositionContractMismatchException
                    if(metadataViewType.IsInterface)
                    {
                        if(ex.InnerException.GetType() == typeof(InvalidCastException))
                        {
                            // Unable to create an Instance of the Metadata view {0} because the exporter exported the metadata for the item {1} with the value {2} as type {3} but the view imports it as type {4}.
                            throw new CompositionContractMismatchException(string.Format(CultureInfo.CurrentCulture, 
                                Strings.ContractMismatch_InvalidCastOnMetadataField,
                                ex.InnerException.Data[MetadataViewGenerator.MetadataViewType],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemKey],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemValue],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemSourceType],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemTargetType]), ex);
                        }
                        else if (ex.InnerException.GetType() == typeof(NullReferenceException))
                        {
                            // Unable to create an Instance of the Metadata view {0} because the exporter exported the metadata for the item {1} with a null value and null is not a valid value for type {2}.
                            throw new CompositionContractMismatchException(string.Format(CultureInfo.CurrentCulture,
                                Strings.ContractMismatch_NullReferenceOnMetadataField,
                                ex.InnerException.Data[MetadataViewGenerator.MetadataViewType],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemKey],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemTargetType]), ex);
                        }
                    }
                    throw;
                }
            }
        }

        public static bool IsViewTypeValid(Type metadataViewType)
        {
            Assumes.NotNull(metadataViewType);

            // If the Metadata dictionary is cast compatible with the passed in type
            if (ExportServices.IsDefaultMetadataViewType(metadataViewType)
            ||  metadataViewType.IsInterface
            ||  ExportServices.IsDictionaryConstructorViewType(metadataViewType))
            {
                return true;
            }

            return false;
        }
    }
}
