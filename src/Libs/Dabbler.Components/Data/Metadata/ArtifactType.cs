// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Classifies application artifacts accessible from the top menu bar.
/// </summary>
public enum ArtifactType
{
    /// <summary>A data entry/editing form bound to an entity.</summary>
    Form,

    /// <summary>A query page displaying filtered result sets.</summary>
    Query,

    /// <summary>A formatted report with parameter-driven output.</summary>
    Report,

    /// <summary>A dashboard or switchboard page with embedded widgets.</summary>
    Dashboard
}
