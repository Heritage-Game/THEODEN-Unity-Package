using System;
using System.Collections.Generic;
using System.Linq;

namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Collects the issues found while validating a THEODEN project.
    /// </summary>
    public sealed class TheodenValidationReport
    {
        private readonly List<ValidationIssue> _issues = new();

        /// <summary>
        /// Gets all issues currently contained in the report.
        /// </summary>
        public IReadOnlyList<ValidationIssue> Issues => _issues;

        /// <summary>
        /// Gets whether the report contains at least one blocking error.
        /// </summary>
        public bool HasErrors =>
            _issues.Any(issue =>
                issue.Severity == ValidationSeverity.Error);

        /// <summary>
        /// Gets whether the validated project can proceed to the build phase.
        /// </summary>
        public bool IsValid => !HasErrors;

        /// <summary>
        /// Gets the number of blocking errors contained in the report.
        /// </summary>
        public int ErrorCount =>
            _issues.Count(issue =>
                issue.Severity == ValidationSeverity.Error);

        /// <summary>
        /// Gets the number of non-blocking warnings contained in the report.
        /// </summary>
        public int WarningCount =>
            _issues.Count(issue =>
                issue.Severity == ValidationSeverity.Warning);

        /// <summary>
        /// Adds an existing issue to the report.
        /// </summary>
        /// <param name="issue">The validation issue to add.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="issue"/> is null.
        /// </exception>
        public void AddIssue(ValidationIssue issue)
        {
            if (issue == null)
                throw new ArgumentNullException(nameof(issue));

            _issues.Add(issue);
        }

        /// <summary>
        /// Adds a blocking error to the report.
        /// </summary>
        /// <param name="code">A stable identifier describing the error.</param>
        /// <param name="message">A user-facing description of the error.</param>
        /// <param name="assetPath">
        /// The Unity project-relative path associated with the error, if available.
        /// </param>
        public void AddError(
            string code,
            string message,
            string assetPath = "")
        {
            AddIssue(new ValidationIssue(
                code,
                ValidationSeverity.Error,
                message,
                assetPath
            ));
        }

        /// <summary>
        /// Adds a non-blocking warning to the report.
        /// </summary>
        /// <param name="code">A stable identifier describing the warning.</param>
        /// <param name="message">A user-facing description of the warning.</param>
        /// <param name="assetPath">
        /// The Unity project-relative path associated with the warning, if available.
        /// </param>
        public void AddWarning(
            string code,
            string message,
            string assetPath = "")
        {
            AddIssue(new ValidationIssue(
                code,
                ValidationSeverity.Warning,
                message,
                assetPath
            ));
        }
    }
}