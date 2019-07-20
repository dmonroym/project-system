﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public sealed class IDependencyModelEqualityComparerTests
    {
        [Fact]
        public void EqualsAndGetHashCode()
        {
            var model1 = new TestDependencyModel
            {
                Id = "id1",
                ProviderType = "provider1"
            };

            var model2 = new TestDependencyModel
            {
                Id = "id1",
                ProviderType = "provider1"
            };

            var model3 = new TestDependencyModel
            {
                Id = "DIFFERENT",
                ProviderType = "provider1"
            };

            var model4 = new TestDependencyModel
            {
                Id = "id1",
                ProviderType = "DIFFERENT"
            };

            Assert.True(IDependencyModelEqualityComparer.Instance.Equals(model1, model2));
            Assert.True(IDependencyModelEqualityComparer.Instance.Equals(model2, model1));

            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model1, model3));
            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model3, model1));

            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model1, model4));
            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model4, model1));

            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model3, model4));
            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model4, model3));

            Assert.Equal(
                IDependencyModelEqualityComparer.Instance.GetHashCode(model1),
                IDependencyModelEqualityComparer.Instance.GetHashCode(model2));

            Assert.NotEqual(
                IDependencyModelEqualityComparer.Instance.GetHashCode(model1),
                IDependencyModelEqualityComparer.Instance.GetHashCode(model3));

            Assert.NotEqual(
                IDependencyModelEqualityComparer.Instance.GetHashCode(model1),
                IDependencyModelEqualityComparer.Instance.GetHashCode(model4));

            Assert.NotEqual(
                IDependencyModelEqualityComparer.Instance.GetHashCode(model3),
                IDependencyModelEqualityComparer.Instance.GetHashCode(model4));
        }
    }
}
