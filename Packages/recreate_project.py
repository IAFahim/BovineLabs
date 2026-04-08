import os

def write(path, content):
    os.makedirs(os.path.dirname(path), exist_ok=True) if os.path.dirname(path) else None
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(content)

# ── Bovinelabs.Timeline.Entity.Links.Authoring ──────────────────────────────

write("Bovinelabs.Timeline.Entity.Links.Authoring/AssemblyInfo.cs", """\
using System.Runtime.CompilerServices;
using Unity.Entities;

[assembly: DisableAutoTypeRegistration]

[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Editor")]
[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Tests")]
""")

write("Bovinelabs.Timeline.Entity.Links.Authoring/Bovinelabs.Timeline.Entity.Links.Authoring.asmdef", """\
{
    "name": "Bovinelabs.Timeline.Entity.Links.Authoring",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Authoring",
        "BovineLabs.Core.Extensions",
        "BovineLabs.Core.Extensions.Authoring",
        "Bovinelabs.Timeline.Entity.Links.Data",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_EDITOR"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
""")

# ── Bovinelabs.Timeline.Entity.Links.Data ───────────────────────────────────

write("Bovinelabs.Timeline.Entity.Links.Data/AssemblyInfo.cs", """\
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links")]
[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Authoring")]
[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Debug")]
[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Editor")]
[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Tests")]
""")

write("Bovinelabs.Timeline.Entity.Links.Data/Bovinelabs.Timeline.Entity.Links.Data.asmdef", """\
{
    "name": "Bovinelabs.Timeline.Entity.Links.Data",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Extensions",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
""")

# ── Bovinelabs.Timeline.Entity.Links.Debug ──────────────────────────────────

write("Bovinelabs.Timeline.Entity.Links.Debug/Bovinelabs.Timeline.Entity.Links.Debug.asmdef", """\
{
    "name": "Bovinelabs.Timeline.Entity.Links.Debug",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Anchor",
        "BovineLabs.Core",
        "BovineLabs.Core.Extensions",
        "Bovinelabs.Timeline.Entity.Links",
        "Bovinelabs.Timeline.Entity.Links.Data",
        "Unity.AppUI",
        "Unity.AppUI.MVVM",
        "Unity.AppUI.Navigation",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_EDITOR || BL_DEBUG"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
""")

# ── Bovinelabs.Timeline.Entity.Links.Editor ─────────────────────────────────

write("Bovinelabs.Timeline.Entity.Links.Editor/Bovinelabs.Timeline.Entity.Links.Editor.asmdef", """\
{
    "name": "Bovinelabs.Timeline.Entity.Links.Editor",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Editor",
        "BovineLabs.Core.Extensions",
        "BovineLabs.Core.Extensions.Editor",
        "Bovinelabs.Timeline.Entity.Links",
        "Bovinelabs.Timeline.Entity.Links.Authoring",
        "Bovinelabs.Timeline.Entity.Links.Data",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
""")

# ── Bovinelabs.Timeline.Entity.Links.Tests ──────────────────────────────────

write("Bovinelabs.Timeline.Entity.Links.Tests/AssemblyInfo.cs", """\
using System.Runtime.CompilerServices;
using Unity.Entities;

[assembly: DisableAutoCreation]
""")

write("Bovinelabs.Timeline.Entity.Links.Tests/Bovinelabs.Timeline.Entity.Links.Tests.asmdef", """\
{
    "name": "Bovinelabs.Timeline.Entity.Links.Tests",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Extensions",
        "BovineLabs.Testing",
        "Bovinelabs.Timeline.Entity.Links",
        "Bovinelabs.Timeline.Entity.Links.Data",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.PerformanceTesting",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [
        "TestAssemblies"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
""")

# ── Bovinelabs.Timeline.Entity.Links (runtime) ──────────────────────────────

write("Bovinelabs.Timeline.Entity.Links/AssemblyInfo.cs", """\
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Debug")]
[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Editor")]
[assembly: InternalsVisibleTo("Bovinelabs.Timeline.Entity.Links.Tests")]
""")

write("Bovinelabs.Timeline.Entity.Links/Bovinelabs.Timeline.Entity.Links.asmdef", """\
{
    "name": "Bovinelabs.Timeline.Entity.Links",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Anchor",
        "BovineLabs.Core",
        "BovineLabs.Core.Extensions",
        "Bovinelabs.Timeline.Entity.Links.Data",
        "Unity.AppUI",
        "Unity.AppUI.MVVM",
        "Unity.AppUI.Navigation",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
""")

# ── Root files ───────────────────────────────────────────────────────────────

write("CHANGELOG.md", """\
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-04-08

### This is the first release of *\\<Bovinelabs Timeline Entity Links\\>*.

*Short description of this release*
""")

write("README.md", """\
Use this file to describe your package's features.
""")

write("Third Party Notices.md", """\
This package contains third-party software components governed by the license(s) indicated below:
---------

Component Name: [provide component name]

License Type: [Provide license type, i.e. "MIT", "Apache 2.0"]

[Provide License Details]

---------
Component Name: [provide component name]

License Type: [Provide license type, i.e. "MIT", "Apache 2.0"]

[Provide License Details]

""")

write("package.json", """\
{
\t"name": "com.fahimmanowarj5.bovinelabstimelineentitylinks",
\t"displayName":"Bovinelabs Timeline Entity Links",
\t"version": "0.1.0",
\t"unity": "6000.6",
\t"unityRelease": "0a2",
\t"description": "Replace this with your own description of the package. \\n\\nFor best results, use this text to summarize: \\n\\u25AA What the package does \\n\\u25AA How it can benefit the user \\n\\nNote: Special formatting characters are supported, including line breaks ('\\\\n') and bullets ('\\\\u25AA').",
\t"dependencies": {
\t\t"com.unity.test-framework": "1.6.0"
\t},
\t"author": {
\t\t"name": "PRO_GrAMmER PRO_GrAMmER (PRO_GrAMmER)",
\t\t"url": "http://www.example.com",
\t\t"email": "example@email.com"
\t},
\t"changelogUrl": "https://example.com/changelog.html",
\t"documentationUrl": "https://example.com/",
\t"licensesUrl": "https://example.com/licensing.html"
}
""")

# ── Documentation ────────────────────────────────────────────────────────────

write("Documentation/Bovinelabs Timeline Entity Links.md", """\
>>>
**_Package Documentation Template_**

Use this template to create preliminary, high-level documentation meant to introduce users to the feature and the sample files included in this package. When writing your documentation, do the following:

1. Follow instructions in blockquotes.

2. Replace angle brackets with the appropriate text. For example, replace "&lt;package name&gt;" with the official name of the package.
 
3. Delete sections that do not apply to your package. For example, a package containing only sample files does not have a "Using &lt;package_name&gt;" section, so this section can be removed.
 
4. After documentation is completed, make sure you delete all instructions and examples in blockquotes including this preamble and its title:

\t\t```
\t\t>>>
\t\tDelete all of the text between pairs of blockquote markdown.
\t\t>>>
\t\t```
>>>

# About &lt;package name&gt;

>>>
Name the heading of the first topic after the **displayName** of the package as it appears in the package manifest.

This first topic includes a brief, high-level explanation of the package and, if applicable, provides links to Unity Manual topics.

There are two types of packages:

 - Packages that include features that augment the Unity Editor or Runtime.
 - Packages that include sample files.

Choose one of the following introductory paragraphs that best fits the package:
>>>

Use the &lt;package name&gt; package to &lt;list of the main uses for the package&gt;. For example, use &lt;package name&gt; to create/generate/extend/capture &lt;mention major use case, or a good example of what the package can be used for&gt;. The &lt;package name&gt; package also includes &lt;other relevant features or uses&gt;.

> *or*

The &lt;package name&gt; package includes examples of &lt;name of asset type, model, prefabs, and/or other GameObjects in the package&gt;. For more information, see &lt;xref to topic in the Unity Manual&gt;.

>>>
**_Examples:_** 

Here are some examples for reference only. Do not include these in the final documentation file:

*Use the Unity Recorder package to capture and save in-game data. For example, use Unity Recorder to record an mp4 file during a game session. The Unity Recorder package also includes an interface for setting-up and triggering recording sessions.*

*The Timeline Examples package includes examples of Timeline assets, Timeline Instances, animation, GameObjects, and scripts that illustrate how to use Unity's Timeline. For more information, see [ Unity's Timeline](https://docs.unity3d.com/Manual/TimelineSection.html) in the [Unity Manual](https://docs.unity3d.com). For licensing and usage, see Package Licensing.*
>>>



# Installing &lt;package name&gt;
>>>
Begin this section with a cross-reference to the official Unity Manual topic on how to install packages. If the package requires special installation instructions, include these steps in this section.
>>>

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html). 

>>>
For some packages, there may be additional steps to complete the setup. You can add those here.
>>>

In addition, you need to install the following resources:

 - &lt;name of resource&gt;: To install, open *Window > &lt;name of menu item&gt;*. The resource appears &lt;at this location&gt;.
 - &lt;name of sample&gt;: To install, open *Window > &lt;name of menu item&gt;*. The new sample folder appears &lt;at this location&gt;.


<a name="UsingPackageName"></a>
# Using &lt;package name&gt;
>>>
The contents of this section depends on the type of package.

For packages that augment the Unity Editor with additional features, this section should include workflow and/or reference documentation:

* At a minimum, this section should include reference documentation that describes the windows, editors, and properties that the package adds to Unity. This reference documentation should include screen grabs (see how to add screens below), a list of settings, an explanation of what each setting does, and the default values of each setting.
* Ideally, this section should also include a workflow: a list of steps that the user can easily follow that demonstrates how to use the feature. This list of steps should include screen grabs (see how to add screens below) to better describe how to use the feature.

For packages that include sample files, this section may include detailed information on how the user can use these sample files in their projects and scenes. However, workflow diagrams or illustrations could be included if deemed appropriate.

## How to add images

*(This section is for reference. Do not include in the final documentation file)* 

If the [Using &lt;package name&gt;](#UsingPackageName) section includes screen grabs or diagrams, a link to the image must be added to this MD file, before or after the paragraph with the instruction or description that references the image. In addition, a caption should be added to the image link that includes the name of the screen or diagram. All images must be PNG files with underscores for spaces. No animated GIFs.

An example is included below:

![A cinematic in the Timeline Editor window.](images/example.png)

Notice that the example screen shot is included in the images folder. All screen grabs and/or diagrams must be added and referenced from the images folder.

For more on the Unity documentation standards for creating and adding screen grabs, see this confluence page: https://confluence.hq.unity3d.com/pages/viewpage.action?pageId=13500715
>>>



# Technical details
## Requirements
>>>
This subtopic includes a bullet list with the compatible versions of Unity. This subtopic may also include additional requirements or recommendations for 3rd party software or hardware. An example includes a dependency on other packages. If you need to include references to non-Unity products, make sure you refer to these products correctly and that all references include the proper trademarks (tm or r)
>>>

This version of &lt;package name&gt; is compatible with the following versions of the Unity Editor:

* 2018.1 and later (recommended)

To use this package, you must have the following 3rd party products:

* &lt;product name and version with trademark or registered trademark.&gt;
* &lt;product name and version with trademark or registered trademark.&gt;
* &lt;product name and version with trademark or registered trademark.&gt;

## Known limitations
>>>
This section lists the known limitations with this version of the package. If there are no known limitations, or if the limitations are trivial, exclude this section. An example is provided.
>>>

&lt;package name&gt; version &lt;package version&gt; includes the following known limitations:

* &lt;brief one-line description of first limitation.&gt;
* &lt;brief one-line description of second limitation.&gt;
* &lt;and so on&gt;

>>>
*Example (For reference. Do not include in the final documentation file):*

The Unity Recorder version 1.0 has the following limitations:*

* The Unity Recorder does not support sound.
* The Recorder window and Recorder properties are not available in standalone players.
* MP4 encoding is only available on Windows.
>>>

## Package contents
>>>
This section includes the location of important files you want the user to know about. For example, if this is a sample package containing textures, models, and materials separated by sample group, you may want to provide the folder location of each group.
>>>

The following table indicates the &lt;describe the breakdown you used here&gt;:

|Location|Description|
|---|---|
|`<folder>`|Contains &lt;describe what the folder contains&gt;.|
|`<file>`|Contains &lt;describe what the file represents or implements&gt;.|

>>>
*Example (For reference. Do not include in the final documentation file):*

The following table indicates the root folder of each type of sample in this package. Each sample's root folder contains its own Materials, Models, or Textures folders:

|Folder Location|Description|
|---|---|
|`WoodenCrate_Orange`|Root folder containing the assets for the orange crates.|
|`WoodenCrate_Mahogany`|Root folder containing the assets for the mahogany crates.|
|`WoodenCrate_Shared`|Root folder containing any material assets shared by all crates.|
>>>

## Document revision history
>>>
This section includes the revision history of the document. The revision history tracks when a document is created, edited, and updated. If you create or update a document, you must add a new row describing the revision.  The Documentation Team also uses this table to track when a document is edited and its editing level. An example is provided:
 
|Date|Reason|
|---|---|
|Sept 12, 2017|Unedited. Published to package.|
|Sept 10, 2017|Document updated for package version 1.1.<br>New features: <li>audio support for capturing MP4s.<li>Instructions on saving Recorder prefabs|
|Sept 5, 2017|Limited edit by Documentation Team. Published to package.|
|Aug 25, 2017|Document created. Matches package version 1.0.|
>>>
""")

# ── Samples/Scene ────────────────────────────────────────────────────────────

write("Samples/Scene/Scene Bovinelabs Timeline Entity Links.unity", """\
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 10
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 0
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 10304, guid: 0000000000000000f000000000000000, type: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 13
  m_BakeOnSceneLoad: 0
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 1
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 2
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 1
    m_PVRFilteringGaussRadiusAO: 1
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 20201, guid: 0000000000000000f000000000000000, type: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
--- !u!1 &83058835
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 83058838}
  - component: {fileID: 83058837}
  - component: {fileID: 83058836}
  - component: {fileID: 83058839}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!81 &83058836
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 83058835}
  m_Enabled: 1
--- !u!20 &83058837
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 83058835}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {x: 2, y: 11}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!4 &83058838
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 83058835}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 1, z: -10}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &83058839
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 83058835}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: a79441f348de89743a2939f4d699eac1, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.UniversalAdditionalCameraData
  m_RenderShadows: 1
  m_RequiresDepthTextureOption: 2
  m_RequiresOpaqueTextureOption: 2
  m_CameraType: 0
  m_Cameras: []
  m_RendererIndex: -1
  m_VolumeLayerMask:
    serializedVersion: 2
    m_Bits: 1
  m_VolumeTrigger: {fileID: 0}
  m_VolumeFrameworkUpdateModeOption: 2
  m_RenderPostProcessing: 0
  m_Antialiasing: 0
  m_AntialiasingQuality: 2
  m_StopNaN: 0
  m_Dithering: 0
  m_ClearDepth: 1
  m_AllowXRRendering: 1
  m_AllowHDROutput: 1
  m_UseScreenCoordOverride: 0
  m_ScreenSizeOverride: {x: 0, y: 0, z: 0, w: 0}
  m_ScreenCoordScaleBias: {x: 0, y: 0, z: 0, w: 0}
  m_RequiresDepthTexture: 0
  m_RequiresColorTexture: 0
  m_TaaSettings:
    m_Quality: 3
    m_FrameInfluence: 0.1
    m_JitterScale: 1
    m_MipBias: 0
    m_VarianceClampScale: 0.9
    m_ContrastAdaptiveSharpening: 0
  m_Version: 2
--- !u!1 &102849032
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 102849033}
  - component: {fileID: 102849034}
  m_Layer: 0
  m_Name: Sub Scene
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &102849033
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 102849032}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &102849034
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 102849032}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 45a335734b1572644a6a5d09d87adc65, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.Scenes::Unity.Scenes.SubScene
  _SceneAsset: {fileID: 102900000, guid: cee56de481622a20a93e3e6f078ff5ed, type: 3}
  _HierarchyColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  AutoLoadScene: 1
  _SceneGUID:
    Value:
      x: 1322671852
      y: 44181016
      z: 4142130074
      w: 3730831472
--- !u!1 &684391987
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 684391989}
  - component: {fileID: 684391988}
  - component: {fileID: 684391990}
  m_Layer: 0
  m_Name: Directional Light
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!108 &684391988
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 684391987}
  m_Enabled: 1
  serializedVersion: 13
  m_Type: 1
  m_Color: {r: 1, g: 0.95686275, b: 0.8392157, a: 1}
  m_Intensity: 1
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.80208
  m_CookieSize2D: {x: 0.5, y: 0.5}
  m_Shadows:
    m_Type: 2
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 1
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
    m_CullingMatrixOverride:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
    m_UseCullingMatrixOverride: 0
  m_Cookie: {fileID: 0}
  m_DrawHalo: 0
  m_Flare: {fileID: 0}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {x: 1, y: 1}
  m_BounceIntensity: 1
  m_ColorTemperature: 6570
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {x: 0, y: 0, z: 0, w: 0}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_ForceVisible: 0
  m_ShapeRadius: 0.025
  m_ShadowAngle: 0
  m_LightUnit: 1
  m_LuxAtDistance: 1
  m_EnableSpotReflector: 1
--- !u!4 &684391989
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 684391987}
  serializedVersion: 2
  m_LocalRotation: {x: 0.40821788, y: -0.23456968, z: 0.10938163, w: 0.8754261}
  m_LocalPosition: {x: 0, y: 3, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 50, y: -30, z: 0}
--- !u!114 &684391990
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 684391987}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 474bcb49853aa07438625e644c072ee6, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.UniversalAdditionalLightData
  m_UsePipelineSettings: 1
  m_AdditionalLightsShadowResolutionTier: 2
  m_CustomShadowLayers: 0
  m_LightCookieSize: {x: 1, y: 1}
  m_LightCookieOffset: {x: 0, y: 0}
  m_SoftShadowQuality: 0
  m_RenderingLayersMask:
    serializedVersion: 0
    m_Bits: 1
  m_ShadowRenderingLayersMask:
    serializedVersion: 0
    m_Bits: 1
  m_Version: 4
  m_LightLayerMask: 1
  m_ShadowLayerMask: 1
  m_RenderingLayers: 1
  m_ShadowRenderingLayers: 1
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {fileID: 83058838}
  - {fileID: 684391989}
  - {fileID: 102849033}
""")

write("Samples/Scene/Sub Scene Bovinelabs Timeline Entity Links.unity", """\
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 10
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 0
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 10304, guid: 0000000000000000f000000000000000, type: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 13
  m_BakeOnSceneLoad: 0
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 1
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 2
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 1
    m_PVRFilteringGaussRadiusAO: 1
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 20201, guid: 0000000000000000f000000000000000, type: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
--- !u!1 &1866941876
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 1866941878}
  - component: {fileID: 1866941877}
  m_Layer: 0
  m_Name: Settings
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!114 &1866941877
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1866941876}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 658a6f8e8db44242a893e1a0098a597b, type: 3}
  m_Name: 
  m_EditorClassIdentifier: BovineLabs.Core.Authoring::BovineLabs.Core.Authoring.Settings.SettingsAuthoring
  settings:
  - {fileID: 11400000, guid: 2c3495472aaa942d7a34e8bcef68cab4, type: 2}
  - {fileID: 11400000, guid: 1288f2da12d38259bba0a73616fcefcb, type: 2}
--- !u!4 &1866941878
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1866941876}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {fileID: 1866941878}
""")

print("Done. All files created successfully.")
