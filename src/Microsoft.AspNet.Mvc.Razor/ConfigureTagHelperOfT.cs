﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Configures an <see cref="ITagHelper"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConfigureTagHelper<T> : IConfigureTagHelper<T>
        where T : ITagHelper
    {
        /// <summary>
        /// Creates an <see cref="Configure(ITagHelper, ViewContext)"/>.
        /// </summary>
        /// <param name="action">The configuration delegate.</param>
        public ConfigureTagHelper(Action<T, ViewContext> action)
        {
            Action = action;
        }

        /// <summary>
        /// The configuration delegate.
        /// </summary>
        public Action<T, ViewContext> Action { get; }

        /// <summary>
        /// Configures the <see cref="ITagHelper"/> using <see cref="Action"/>;
        /// </summary>
        /// <param name="helper">The <see cref="ITagHelper"/> to configure.</param>
        /// <param name="context">
        ///     The <see cref="ViewContext"/> for the <see cref="IView"/> the <see cref="ITagHelper"/> is in.
        /// </param>
        public void Configure(ITagHelper helper, ViewContext context)
        {
            if (!helper.GetType().IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException("", "helper");
            }

            Action((T)helper, context);
        }
    }
}