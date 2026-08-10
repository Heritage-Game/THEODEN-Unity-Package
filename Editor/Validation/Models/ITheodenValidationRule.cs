namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Defines a validation rule that inspects a THEODEN project
    /// and adds any detected issues to a validation report.
    /// </summary>
    /// <remarks>
    /// Implementations must not modify the project, its configuration,
    /// or its assets. A rule should report every issue it finds instead
    /// of stopping after the first one.
    /// </remarks>
    public interface ITheodenValidationRule
    {
        /// <summary>
        /// Validates a specific aspect of a THEODEN project.
        /// </summary>
        /// <param name="context">
        /// The context of the project selected as the validation target.
        /// </param>
        /// <param name="report">
        /// The report to which detected errors and warnings must be added.
        /// </param>
        void Validate(
            TheodenProjectContext context,
            TheodenValidationReport report
        );
    }
}