﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.NodejsTools.TypeScript;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.NodejsTools.TestAdapter
{
    internal static class VSProjectExtensions
    {
        internal static EnvDTE.Project GetProject(this IVsHierarchy hierarchy)
        {

            ErrorHandler.ThrowOnFailure(
                hierarchy.GetProperty(
                    VSConstants.VSITEMID_ROOT,
                    (int)__VSHPROPID.VSHPROPID_ExtObject,
                    out var project
                )
            );

            return (project as EnvDTE.Project);
        }

        /// <summary>
        /// Gets the name of the project.
        /// </summary>
        public static string GetProjectName(this IVsProject project)
        {
            ValidateArg.NotNull(project, "project");

            var projectHierarchy = (IVsHierarchy)project;
            ErrorHandler.ThrowOnFailure(projectHierarchy.GetProperty((uint)VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out var projectName));
            return (string)projectName;
        }

        public static bool TryGetProjectPath(this IVsProject project, out string path)
        {
            ValidateArg.NotNull(project, "project");

            return ErrorHandler.Succeeded(project.GetMkDocument(VSConstants.VSITEMID_ROOT, out path)) && !string.IsNullOrEmpty(path);
        }

        public static bool TryGetProjectDirectory(this IVsProject project, out string path)
        {
            var succeeded = TryGetProjectPath(project, out path);

            if (succeeded && TypeScriptHelpers.IsSupportedTestProjectFile(path))
            {
                // remove project name from the path.
                path = Path.GetDirectoryName(path);
            }

            return succeeded;
        }

        private static string GetAggregateProjectTypeGuids(this IVsProject project)
        {
            ValidateArg.NotNull(project, "project");

            var aggregatableProject = project as IVsAggregatableProject;
            var aggregatableProjectCorrected = project as IVsAggregatableProjectCorrected;

            // Then it is not an aggregated project
            if (aggregatableProject == null && aggregatableProjectCorrected == null)
            {
                return string.Empty;
            }

            var projectTypeGuids = string.Empty;

            if (aggregatableProject != null)
            {
                ErrorHandler.ThrowOnFailure(aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids));
            }
            else if (aggregatableProjectCorrected != null)
            {
                ErrorHandler.ThrowOnFailure(aggregatableProjectCorrected.GetAggregateProjectTypeGuids(out projectTypeGuids));
            }

            return projectTypeGuids;
        }

        /// <summary>
        /// Gets the project home directory.
        /// </summary>
        public static string GetProjectHome(this IVsProject project)
        {
            Debug.Assert(project != null);
            var hier = (IVsHierarchy)project;
            ErrorHandler.ThrowOnFailure(hier.GetProperty(
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_ExtObject,
                out var extObject
            ));
            var proj = extObject as EnvDTE.Project;
            if (proj == null)
            {
                return null;
            }
            var props = proj.Properties;
            if (props == null)
            {
                return null;
            }

            try
            {
                var projHome = props.Item("ProjectHome");
                return projHome == null ? null : projHome.Value as string;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the file paths of items in the project.
        /// </summary>
        public static IEnumerable<string> GetProjectItemPaths(this IVsProject project)
        {
            ErrorHandler.ThrowOnFailure(project.GetMkDocument(VSConstants.VSITEMID_ROOT, out var path));
            if (string.IsNullOrEmpty(path))
            {
                yield break;
            }

            yield return path;
            foreach (var filePath in project.GetProjectItems())
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    yield return filePath;
                }
            }
        }

        /// <summary>
        /// Get the items present in the project
        /// </summary>
        private static IEnumerable<string> GetProjectItems(this IVsProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            // Each item in VS OM is IVSHierarchy. 
            var hierarchy = (IVsHierarchy)project;

            return GetProjectItems(hierarchy, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Get project items
        /// </summary>
        private static IEnumerable<string> GetProjectItems(IVsHierarchy project, uint itemId)
        {
            for (var childId = GetItemId(GetPropertyValue((int)__VSHPROPID.VSHPROPID_FirstChild, itemId, project));
                childId != VSConstants.VSITEMID_NIL;
                childId = GetItemId(GetPropertyValue((int)__VSHPROPID.VSHPROPID_NextSibling, childId, project)))
            {
                if ((GetPropertyValue((int)__VSHPROPID.VSHPROPID_IsNonMemberItem, childId, project) as bool?) ?? false)
                {
                    continue;
                }

                foreach (var item in GetProjectItems(project, childId))
                {
                    yield return item;
                }

                var childPath = GetCanonicalName(childId, project);
                yield return childPath;
            }
        }

        /// <summary>
        /// Convert parameter object to ItemId
        /// </summary>
        private static uint GetItemId(object pvar)
        {
            if (pvar == null)
            {
                return VSConstants.VSITEMID_NIL;
            }

            if (pvar is int)
            {
                return (uint)(int)pvar;
            }

            if (pvar is uint)
            {
                return (uint)pvar;
            }

            if (pvar is short)
            {
                return (uint)(short)pvar;
            }

            if (pvar is ushort)
            {
                return (uint)(ushort)pvar;
            }

            if (pvar is long)
            {
                return (uint)(long)pvar;
            }

            return VSConstants.VSITEMID_NIL;
        }

        /// <summary>
        /// Get the parameter property value
        /// </summary>
        private static object GetPropertyValue(int propid, uint itemId, IVsHierarchy vsHierarchy)
        {
            if (itemId == VSConstants.VSITEMID_NIL)
            {
                return null;
            }

            if (ErrorHandler.Failed(vsHierarchy.GetProperty(itemId, propid, out var o)))
            {
                return null;
            }
            return o;
        }

        /// <summary>
        /// Get the canonical name
        /// </summary>
        private static string GetCanonicalName(uint itemId, IVsHierarchy hierarchy)
        {
            Debug.Assert(itemId != VSConstants.VSITEMID_NIL, "ItemId cannot be nill");

            var strRet = string.Empty;
            if (ErrorHandler.Failed(hierarchy.GetCanonicalName(itemId, out strRet)))
            {
                return string.Empty;
            }
            return strRet;
        }
    }
}
