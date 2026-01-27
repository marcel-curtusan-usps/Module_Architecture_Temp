# ContainerDetectionModule

A microservice for computer vision and object detection, specifically designed for USPS container detection with support for multiple vision analysis methods.

## Overview

ContainerDetectionModule provides three distinct API endpoints for image analysis:

1. **Custom Detection** (`/api/detect`) - USPS-specific object detection
2. **Built-in Vision** (`/api/builtin-vision`) - .NET 10 Computer Vision API integration
3. **Foundry Vision** (`/api/foundry-vision`) - Microsoft Foundry Computer Vision API proxy

## Architecture

The module follows the standard microservice architecture pattern used in this solution:
- **Controllers**: Handle HTTP requests and responses
- **Services**: Contain business logic and external API integrations
- **Models**: Define request/response structures

## API Endpoints

### 1. POST /api/detect
Custom object detection for USPS containers, people, and vehicles.

**Request:**
```json
{
  "base64Image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "fileName": "container.jpg",
  "contentType": "image/jpeg"
}
```

**Response:**
```json
{
  "success": true,
  "detectedObjects": [
    {
      "type": "USPSContainer",
      "confidence": 0.92,
      "boundingBox": { "x": 100, "y": 150, "width": 200, "height": 180 },
      "properties": {
        "containerType": "MailBin",
        "estimatedCapacity": "Large",
        "color": "Blue"
      }
    }
  ],
  "processingTimeMs": 150,
  "message": "Detection completed successfully"
}
```

**TODO Items:**
- [ ] Load and integrate actual ML model (ONNX, TensorFlow, PyTorch)
- [ ] Implement image preprocessing pipeline
- [ ] Add model inference logic
- [ ] Implement non-maximum suppression (NMS)
- [ ] Add confidence thresholding configuration
- [ ] Set up model versioning and updates

### 2. POST /api/builtin-vision
Image analysis using .NET 10 built-in Computer Vision APIs.

**Request:**
```json
{
  "base64Image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "fileName": "scene.jpg",
  "contentType": "image/jpeg"
}
```

**Response:**
```json
{
  "success": true,
  "objects": [
    {
      "name": "person",
      "confidence": 0.91,
      "boundingBox": { "x": 120, "y": 80, "width": 100, "height": 200 }
    }
  ],
  "tags": ["outdoor", "vehicle", "person", "building"],
  "description": "A delivery truck parked near a building with a person nearby.",
  "processingTimeMs": 120,
  "message": "Analysis completed successfully"
}
```

**TODO Items:**
- [ ] Install required NuGet packages for .NET Computer Vision
- [ ] Configure API credentials/endpoints
- [ ] Implement actual API client integration
- [ ] Add error handling for API failures
- [ ] Implement rate limiting and retry logic
- [ ] Add request caching for performance

### 3. POST /api/foundry-vision
Proxy to Microsoft Foundry Computer Vision API for comprehensive analysis.

**Request:**
```json
{
  "base64Image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "fileName": "image.jpg",
  "contentType": "image/jpeg"
}
```

**Response:**
```json
{
  "success": true,
  "analysis": {
    "detectedObjects": [
      {
        "name": "mailbox",
        "confidence": 0.94,
        "boundingBox": { "x": 50, "y": 100, "width": 150, "height": 200 }
      }
    ],
    "sceneAnalysis": {
      "scene": "outdoor_street",
      "dominantColors": ["Blue", "Gray", "White"],
      "isAdultContent": false,
      "confidence": 0.91
    },
    "ocrResults": [
      {
        "text": "USPS",
        "confidence": 0.98,
        "boundingBox": { "x": 420, "y": 215, "width": 80, "height": 40 }
      }
    ]
  },
  "processingTimeMs": 180,
  "message": "Analysis completed successfully"
}
```

**TODO Items:**
- [ ] Set up Microsoft Foundry API authentication (OAuth/API key)
- [ ] Configure endpoint URLs and API version
- [ ] Implement secure credential storage (Azure Key Vault)
- [ ] Add HttpClient with Polly policies (retry, circuit breaker)
- [ ] Implement request/response mapping
- [ ] Add comprehensive error handling
- [ ] Set up telemetry and monitoring
- [ ] Handle API rate limits and quotas

## Health Check Endpoints

Each service provides a health check endpoint:
- `GET /api/detect/health`
- `GET /api/builtin-vision/health`
- `GET /api/foundry-vision/health`

## Running the Module

The module is automatically discovered and launched by MainApp when configured in `MainApp/appsettings.json`:

```json
{
  "Name": "ContainerDetectionModule",
  "Port": 6003,
  "ProjectPath": null
}
```

To run standalone:
```bash
dotnet run --project ContainerDetectionModule
```

## Development Status

⚠️ **Current Status: STUB IMPLEMENTATION**

All endpoints currently return mock/stub data. This is intentional to provide:
- Complete API structure and contracts
- Example request/response patterns
- Service architecture framework
- Clear TODO markers for implementation

## Next Steps for Implementation

1. **Custom Detection Service**
   - Choose ML framework (ONNX Runtime, ML.NET, TensorFlow.NET)
   - Train or acquire pre-trained model for USPS container detection
   - Implement inference pipeline

2. **Built-in Vision Service**
   - Research available .NET 10 Computer Vision APIs
   - Install required packages
   - Implement API integration

3. **Foundry Vision Service**
   - Obtain Microsoft Foundry API credentials
   - Set up authentication flow
   - Implement HTTP client with proper error handling

4. **Testing**
   - Add unit tests for services
   - Add integration tests for endpoints
   - Set up performance benchmarks

5. **Production Readiness**
   - Add comprehensive logging
   - Implement metrics and monitoring
   - Set up health checks
   - Add API documentation
   - Configure deployment pipeline

## Dependencies

- .NET 10.0
- Microsoft.AspNetCore.OpenApi (10.0.2)
- Scalar.AspNetCore (1.2.64)

## Port Configuration

Default port: **6003** (configured in MainApp's appsettings.json)

## Interactive API Documentation

Access the interactive API documentation at:
- OpenAPI: `http://localhost:6003/openapi/v1.json`
- Scalar UI: `http://localhost:6003/` (root path)
