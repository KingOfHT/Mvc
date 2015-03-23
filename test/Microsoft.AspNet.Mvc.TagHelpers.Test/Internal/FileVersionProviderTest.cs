﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Expiration.Interfaces;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    public class FileVersionProviderTest
    {
        [Fact]
        public void AddsVersionToFiles_WhenCacheIsAbsent()
        {
            // Arrange
            var appName = "testApp";
            var filePath = "/hello/world";
            var hostingEnvironment = GetMockHostingEnvironment(filePath);
            var fileVersionProvider = new FileVersionProvider(hostingEnvironment.WebRootFileProvider, appName, null);

            // Act
            var result = fileVersionProvider.AddVersionToFilePath(filePath);

            // Assert
            Assert.Equal("/hello/world?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
        }

        [Fact]
        public void AddsVersionToFiles_WhenAppNameIsInUrl()
        {
            // Arrange
            var appName = "testApp";
            var filePath = "/testApp/hello/world";
            var hostingEnvironment = GetMockHostingEnvironment(filePath, true);
            var fileVersionProvider = new FileVersionProvider(hostingEnvironment.WebRootFileProvider, appName , null);

            // Act
            var result = fileVersionProvider.AddVersionToFilePath(filePath);

            // Assert
            Assert.Equal("/testApp/hello/world?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
        }

        [Fact]
        public void DoesNotAddVersion_IfFileNotFound()
        {
            // Arrange
            var appName = "testApp";
            var filePath = "http://contoso.com/hello/world";
            var hostingEnvironment = GetMockHostingEnvironment(filePath, false, true);
            var fileVersionProvider = new FileVersionProvider(hostingEnvironment.WebRootFileProvider, appName, null);

            // Act
            var result = fileVersionProvider.AddVersionToFilePath(filePath);

            // Assert
            Assert.Equal("http://contoso.com/hello/world", result);
        }

        [Fact]
        public void ReturnsValueFromCache()
        {
            // Arrange
            var appName = "testApp";
            var filePath = "/hello/world";
            var hostingEnvironment = GetMockHostingEnvironment(filePath);
            var fileVersionProvider = new FileVersionProvider(
                hostingEnvironment.WebRootFileProvider,
                appName,
                GetMockCache("FromCache"));

            // Act
            var result = fileVersionProvider.AddVersionToFilePath(filePath);

            // Assert
            Assert.Equal("FromCache", result);
        }

        [Fact]
        public void SetsValueInCache()
        {
            // Arrange
            var appName = "testApp";
            var filePath = "/hello/world";
            var trigger = new Mock<IExpirationTrigger>();
            var hostingEnvironment = GetMockHostingEnvironment(filePath);
            Mock.Get(hostingEnvironment.WebRootFileProvider)
                .Setup(f => f.Watch(It.IsAny<string>())).Returns(trigger.Object);
            var cache = GetMockCache();
            var cacheSetContext = new Mock<ICacheSetContext>();
            cacheSetContext.Setup(c => c.AddExpirationTrigger(trigger.Object)).Verifiable();
            Mock.Get(cache).Setup(c => c.Set(
                /*key*/ filePath,
                /*link*/ It.IsAny<IEntryLink>(),
                /*state*/ It.IsAny<object>(),
                /*create*/ It.IsAny<Func<ICacheSetContext, object>>()))
                .Returns<string, IEntryLink, object, Func<ICacheSetContext, object>>(
                    (key, link, state, create) =>
                    {
                        cacheSetContext.Setup(c => c.State).Returns(state);
                        return create(cacheSetContext.Object);
                    })
                .Verifiable();
            var fileVersionProvider = new FileVersionProvider(
                hostingEnvironment.WebRootFileProvider,
                appName,
                cache);

            // Act
            var result = fileVersionProvider.AddVersionToFilePath(filePath);

            // Assert
            Assert.Equal("/hello/world?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
            cacheSetContext.VerifyAll();
            Mock.Get(cache).VerifyAll();
        }

        private IHostingEnvironment GetMockHostingEnvironment(
            string filePath,
            bool pathContainsAppName = false,
            bool fileDoesNotExist = false)
        {
            var existingMockFile = new Mock<IFileInfo>();
            existingMockFile.SetupGet(f => f.Exists).Returns(true);
            existingMockFile
                .Setup(m => m.CreateReadStream())
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Hello World!")));

            var nonExistingMockFile = new Mock<IFileInfo>();
            nonExistingMockFile.SetupGet(f => f.Exists).Returns(false);
            nonExistingMockFile
                .Setup(m => m.CreateReadStream())
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Hello World!")));

            var mockFileProvider = new Mock<IFileProvider>();
            if (pathContainsAppName)
            {
                mockFileProvider.Setup(fp => fp.GetFileInfo(filePath)).Returns(nonExistingMockFile.Object);
                mockFileProvider.Setup(fp => fp.GetFileInfo(It.Is<string>(str => str != filePath)))
                    .Returns(existingMockFile.Object);
            }
            else
            {
                mockFileProvider.Setup(fp => fp.GetFileInfo(It.IsAny<string>()))
                    .Returns(fileDoesNotExist? nonExistingMockFile.Object : existingMockFile.Object);
            }

            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.Setup(h => h.WebRootFileProvider).Returns(mockFileProvider.Object);

            return hostingEnvironment.Object;
        }

        private static IMemoryCache GetMockCache(object result = null)
        {
            var cache = new Mock<IMemoryCache>();
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), It.IsAny<IEntryLink>(), out result))
                .Returns(result != null);
            return cache.Object;
        }
    }
}