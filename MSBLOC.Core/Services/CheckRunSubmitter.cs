﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MSBLOC.Core.Interfaces;
using MSBLOC.Core.Model;
using Octokit;

namespace MSBLOC.Core.Services
{
    public class CheckRunSubmitter : ICheckRunSubmitter
    {
        private ICheckRunsClient CheckRunsClient { get; }
        private ILogger<CheckRunSubmitter> Logger { get; }

        public CheckRunSubmitter(ICheckRunsClient checkRunsClient,
            ILogger<CheckRunSubmitter> logger = null)
        {
            CheckRunsClient = checkRunsClient;
            Logger = logger ?? new NullLogger<CheckRunSubmitter>();
        }

        /// <inheritdoc />
        public async Task<CheckRun> SubmitCheckRun(BuildDetails buildDetails,
            string owner, string name, string headSha,
            string checkRunName, string checkRunTitle, string checkRunSummary,
            DateTimeOffset startedAt,
            DateTimeOffset completedAt)
        {
            var newCheckRunAnnotations = buildDetails.Annotations.Select(annotation =>
            {
                var warningLevel = GetCheckWarningLevel(annotation.AnnotationWarningLevel);
                var blobHref = BlobHref(owner, name, headSha, annotation.Filename);
                var newCheckRunAnnotation = new NewCheckRunAnnotation(annotation.Filename, blobHref, annotation.LineNumber, annotation.EndLine, warningLevel, annotation.Message)
                {
                    Title = annotation.Title
                };

                return newCheckRunAnnotation;
            }).ToList();

            var newCheckRun = new NewCheckRun(checkRunName, headSha)
            {
                Output = new NewCheckRunOutput(checkRunTitle, checkRunSummary)
                {
                    Annotations = newCheckRunAnnotations
                },
                Status = CheckStatus.Completed,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                Conclusion = buildDetails.Annotations
                    .Any(annotation => annotation.AnnotationWarningLevel == AnnotationWarningLevel.Failure) ? CheckConclusion.Failure : CheckConclusion.Success
            };

            return await CheckRunsClient.Create(owner, name, newCheckRun);
        }

        private static string BlobHref(string owner, string repository, string sha, string file)
        {
            return $"https://github.com/{owner}/{repository}/blob/{sha}/{file.Replace(@"\", "/")}";
        }
        
        private static CheckWarningLevel GetCheckWarningLevel(AnnotationWarningLevel annotationWarningLevel)
        {
            CheckWarningLevel warningLevel;
            switch (annotationWarningLevel)
            {
                case AnnotationWarningLevel.Warning:
                    warningLevel = CheckWarningLevel.Warning;
                    break;
                case AnnotationWarningLevel.Notice:
                    warningLevel = CheckWarningLevel.Notice;
                    break;
                case AnnotationWarningLevel.Failure:
                    warningLevel = CheckWarningLevel.Failure;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(annotationWarningLevel));
            }

            return warningLevel;
        }
    }
}