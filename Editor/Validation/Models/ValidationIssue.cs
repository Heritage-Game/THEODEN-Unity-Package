namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Describes a single issue found during THEODEN project validation.
    /// </summary>
    public sealed class ValidationIssue
    {
        /// <summary>
        /// Gets the identifier associated with the validation issue.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets the severity of the validation issue.
        /// </summary>
        public ValidationSeverity Severity { get; }

        /// <summary>
        /// Gets the user-facing description of the problem.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the Unity project-relative path associated with the problem.
        /// This value may be empty when the issue is not related to a specific asset.
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        /// Creates a new validation issue.
        /// </summary>
        /// <param name="code">A stable identifier describing the type of issue.</param>
        /// <param name="severity">The severity assigned to the issue.</param>
        /// <param name="message">A user-facing description of the problem.</param>
        /// <param name="assetPath">
        /// The Unity project-relative path associated with the problem, if available.
        /// </param>
        public ValidationIssue(
            string code,
            ValidationSeverity severity,
            string message,
            string assetPath = "")
        {
            Code = code;
            Severity = severity;
            Message = message;
            AssetPath = assetPath ?? "";
        }
    }
}