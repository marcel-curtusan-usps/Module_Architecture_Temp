namespace ContainerDetectionModule.Models;

/// <summary>
/// Base request model for image input. Supports either base64-encoded string or file upload.
/// </summary>
public record ImageRequest
{
    /// <summary>
    /// Base64-encoded image data (optional if file is provided)
    /// </summary>
    public string? Base64Image { get; init; }

    /// <summary>
    /// Image file name (for reference when uploading files)
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Image content type (e.g., "image/jpeg", "image/png")
    /// </summary>
    public string? ContentType { get; init; }
}

/// <summary>
/// Response model for custom container detection endpoint
/// </summary>
public record DetectionResponse
{
    /// <summary>
    /// Indicates if detection was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// List of detected objects in the image
    /// </summary>
    public List<DetectedObject> DetectedObjects { get; init; } = new();

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// Any messages or warnings from the detection process
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Represents a detected object in the image
/// </summary>
public record DetectedObject
{
    /// <summary>
    /// Type of detected object (e.g., "USPSContainer", "Person", "Vehicle")
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Bounding box coordinates (x, y, width, height)
    /// </summary>
    public BoundingBox BoundingBox { get; init; } = new();

    /// <summary>
    /// Additional properties specific to the detected object
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Bounding box for detected objects
/// </summary>
public record BoundingBox
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
}

/// <summary>
/// Response model for .NET built-in Computer Vision API
/// </summary>
public record VisionAnalysisResponse
{
    /// <summary>
    /// Indicates if analysis was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Detected objects and their classifications
    /// </summary>
    public List<VisionObject> Objects { get; init; } = new();

    /// <summary>
    /// Image tags/labels
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Image description (if available)
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// Any messages or warnings
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Object detected by vision API
/// </summary>
public record VisionObject
{
    public string Name { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public BoundingBox? BoundingBox { get; init; }
}

/// <summary>
/// Response model for Microsoft Foundry Computer Vision API
/// </summary>
public record FoundryVisionResponse
{
    /// <summary>
    /// Indicates if the API call was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Analysis results from Foundry API
    /// </summary>
    public FoundryAnalysis? Analysis { get; init; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// Any messages or warnings
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Foundry API analysis results
/// </summary>
public record FoundryAnalysis
{
    /// <summary>
    /// Detected objects with classifications
    /// </summary>
    public List<VisionObject> DetectedObjects { get; init; } = new();

    /// <summary>
    /// Scene analysis results
    /// </summary>
    public Dictionary<string, object> SceneAnalysis { get; init; } = new();

    /// <summary>
    /// OCR results (if text detected)
    /// </summary>
    public List<OcrResult> OcrResults { get; init; } = new();
}

/// <summary>
/// OCR text detection result
/// </summary>
public record OcrResult
{
    public string Text { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public BoundingBox BoundingBox { get; init; } = new();
}
