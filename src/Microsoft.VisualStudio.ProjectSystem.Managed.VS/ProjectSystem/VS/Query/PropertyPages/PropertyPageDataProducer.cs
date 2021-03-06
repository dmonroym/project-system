﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPage"/> instances and populating the requested members.
    /// </summary>
    internal static class PropertyPageDataProducer
    {
        public static IEntityValue CreatePropertyPageValue(IEntityValue parent, IPropertyPageQueryCache cache, QueryProjectPropertiesContext context, Rule rule, List<Rule>? debugChildRules, IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(rule, nameof(rule));

            string name = rule.PageTemplate == "debuggerParent"
                ? DebugUtilities.ConvertRealPageNameToDebugPageName(rule.Name)
                : rule.Name;

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                createKeys());

            return CreatePropertyPageValue(parent.EntityRuntime, identity, cache, context, rule, debugChildRules, requestedProperties);

            IEnumerable<KeyValuePair<string, string>> createKeys()
            {
                yield return new(ProjectModelIdentityKeys.PropertyPageName, name);

                if (context.ItemType is not null)
                {
                    yield return new(ProjectModelIdentityKeys.SourceItemType, context.ItemType);
                }

                if (context.ItemName is not null)
                {
                    yield return new(ProjectModelIdentityKeys.SourceItemName, context.ItemName);
                }
            }
        }

        public static IEntityValue CreatePropertyPageValue(IEntityRuntimeModel runtimeModel, EntityIdentity id, IPropertyPageQueryCache cache, QueryProjectPropertiesContext context, Rule rule, List<Rule>? debugChildRules, IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(rule, nameof(rule));
            var newPropertyPage = new PropertyPageValue(runtimeModel, id, new PropertyPagePropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newPropertyPage.Name = rule.Name;
            }

            if (requestedProperties.DisplayName)
            {
                newPropertyPage.DisplayName = rule.DisplayName;
            }

            if (requestedProperties.Order)
            {
                newPropertyPage.Order = rule.Order;
            }

            if (requestedProperties.Kind)
            {
                newPropertyPage.Kind = rule.PageTemplate == "debuggerParent"
                    ? "generic"
                    : rule.PageTemplate;
            }

            ((IEntityValueFromProvider)newPropertyPage).ProviderState = debugChildRules is not null
                ? new PropertyPageProviderState(cache, context, rule, debugChildRules)
                : new PropertyPageProviderState(cache, context, rule);

            return newPropertyPage;
        }

        public static async Task<IEntityValue?> CreatePropertyPageValueAsync(
            IEntityRuntimeModel runtimeModel,
            EntityIdentity id,
            IProjectService2 projectService,
            IPropertyPageQueryCacheProvider queryCacheProvider,
            QueryProjectPropertiesContext context,
            string propertyPageName,
            IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            propertyPageName = DebugUtilities.ConvertDebugPageNameToRealPageName(propertyPageName);

            if (projectService.GetLoadedProject(context.File) is UnconfiguredProject project
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && projectCatalog.GetSchema(propertyPageName) is Rule rule
                && !rule.PropertyPagesHidden)
            {
                List<Rule>? debugChildRules = null;
                if (rule.PageTemplate == "debuggerParent")
                {
                    debugChildRules = DebugUtilities.GetDebugChildRules(projectCatalog).ToList();
                }

                var propertyPageQueryCache = queryCacheProvider.CreateCache(project);
                IEntityValue propertyPageValue = CreatePropertyPageValue(runtimeModel, id, propertyPageQueryCache, context, rule, debugChildRules, requestedProperties);
                return propertyPageValue;
            }

            return null;
        }

        public static async Task<IEnumerable<IEntityValue>> CreatePropertyPageValuesAsync(
            IEntityValue parent,
            UnconfiguredProject project,
            IPropertyPageQueryCacheProvider queryCacheProvider,
            IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            if (await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog)
            {
                return createPropertyPageValuesAsync();
            }

            return Enumerable.Empty<IEntityValue>();

            IEnumerable<IEntityValue> createPropertyPageValuesAsync()
            {
                Rule? parentDebuggerPageRule = null;

                IPropertyPageQueryCache propertyPageQueryCache = queryCacheProvider.CreateCache(project);
                QueryProjectPropertiesContext context = new QueryProjectPropertiesContext(isProjectFile: true, project.FullPath, itemType: null, itemName: null);
                foreach (string schemaName in projectCatalog.GetProjectLevelPropertyPagesSchemas())
                {
                    if (projectCatalog.GetSchema(schemaName) is Rule rule
                        && !rule.PropertyPagesHidden)
                    {
                        if (rule.PageTemplate == "debuggerParent")
                        {
                            parentDebuggerPageRule = rule;
                        }
                        else if (rule.PageTemplate == "commandNameBasedDebugger")
                        {
                            // Don't do anything here; we don't want a separate page for this Rule.
                            // Rather, its categories and properties will be returned as though
                            // they were part of the top-level debug page.
                        }
                        else
                        {
                            IEntityValue propertyPageValue = CreatePropertyPageValue(parent, propertyPageQueryCache, context, rule, debugChildRules: null, requestedProperties: requestedProperties);
                            yield return propertyPageValue;
                        }
                    }
                }

                if (parentDebuggerPageRule is not null)
                {
                    List<Rule> childDebuggerPageRules = DebugUtilities.GetDebugChildRules(projectCatalog).ToList();
                    IEntityValue propertyPageValue = CreatePropertyPageValue(parent, propertyPageQueryCache, context, parentDebuggerPageRule, debugChildRules: childDebuggerPageRules, requestedProperties: requestedProperties);
                    yield return propertyPageValue;
                }
            }
        }
    }
}
