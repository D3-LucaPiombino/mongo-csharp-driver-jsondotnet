MongoDB .NET Driver Json.NET Integration
========================================

This repository contains the project that supports using Json.NET to serialize/deserialize
documents stored in MongoDB using Json.NET serialization.

It is a standalone project that has an external dependency on the MongoDB .NET Driver.

Compatibility
-------------

This version targets the MongoDB .NET Driver 3.x (`MongoDB.Bson` 3.14.1) on `net10.0` (SDK 10 or later).

### Guid representation

The driver 3.x no longer carries a `GuidRepresentation` on its readers and writers,
so `Guid` values are read and written using a representation configured here:

- `JsonSerializerAdapter.DefaultGuidRepresentation` (default: `Standard`, binary subtype 4).
- `new JsonSerializerAdapter<T>(jsonSerializer, guidRepresentation)` overrides it for one serializer.

Documents that contain legacy `Guid`s (binary subtype 3) are read as `CSharpLegacy`
unless a legacy representation (`JavaLegacy`, `PythonLegacy`) is configured.
Set `DefaultGuidRepresentation = GuidRepresentation.CSharpLegacy` to keep writing the layout of the 2.x defaults.
