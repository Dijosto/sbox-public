using System;
using System.Runtime.InteropServices;

namespace Editor;

public static partial class EditorUtility
{
	/// <summary>
	/// Create a vmdl file from a mesh. Will return non null if the asset was created successfully
	/// </summary>
	public static unsafe Asset CreateModelFromMeshFile( Asset meshFile, string targetAbsolutePath = null )
	{
		var modelFilename = targetAbsolutePath ?? System.IO.Path.ChangeExtension( meshFile.GetSourceFile( true ), ".vmdl" );

		if ( System.IO.File.Exists( modelFilename ) )
			return null;

		// In the future we could just init all tools upfront
		if ( !g_pToolFramework2.InitEngineTool( "modeldoc_editor" ) )
			return null;

		var document = CModelDoc.Create();
		g_pModelDocUtils.InitFromMesh( document, meshFile.Path );
		document.SaveToFile( modelFilename );
		document.DeleteThis();

		var asset = AssetSystem.RegisterFile( modelFilename );
		if ( asset is null )
			return null;

		asset.Compile( true );

		return asset;
	}

	/// <summary>
	/// Create a vmdl file from polygon meshes. Will return non null if the asset was created successfully
	/// </summary>
	public static unsafe Asset CreateModelFromPolygonMeshes( PolygonMesh[] polygonMeshes, string targetAbsolutePath )
	{
		if ( polygonMeshes is null )
			return null;

		if ( polygonMeshes.Length == 0 )
			return null;

		if ( string.IsNullOrWhiteSpace( targetAbsolutePath ) )
			return null;

		if ( !g_pToolFramework2.InitEngineTool( "modeldoc_editor" ) )
			return null;

		var meshes = new List<CModelMesh>();
		foreach ( var polygonMesh in polygonMeshes )
		{
			if ( polygonMesh is null )
				continue;

			var mesh = CModelMesh.Create();
			meshes.Add( mesh );

			var vertices = polygonMesh.VertexHandles.ToArray();
			mesh.AddVertices( vertices.Length );

			var materials = polygonMesh.Materials.ToArray();
			foreach ( var material in materials )
				mesh.AddFaceGroup( material?.Name ?? "dev/helper/testgrid.vmat" );

			mesh.AddFaceGroup( "materials/dev/reflectivity_30.vmat" );
			var invalidGroupIndex = materials.Length;

			var verticesRemap = new Dictionary<int, int>();
			var vertexHandles = polygonMesh.VertexHandles.ToArray();
			for ( var i = 0; i < vertexHandles.Length; i++ )
				verticesRemap.Add( vertexHandles[i].Index, i );

			var positions = vertexHandles.Select( x => polygonMesh.Transform.PointToWorld( polygonMesh.GetVertexPosition( x ) ) )
				.ToArray();

			fixed ( Vector3* pPositions = &positions[0] )
				mesh.SetPositions( (IntPtr)pPositions, positions.Length );

			foreach ( var hFace in polygonMesh.FaceHandles )
			{
				var groupIndex = polygonMesh.GetFaceMaterialIndex( hFace );
				var indices = polygonMesh.GetFaceVertices( hFace )
					.Select( x => verticesRemap[x.Index] )
					.ToArray();

				fixed ( int* pIndices = &indices[0] )
					mesh.AddFace( groupIndex >= 0 ? groupIndex : invalidGroupIndex, (IntPtr)pIndices, indices.Length );
			}

			var uvs = polygonMesh.GetFaceVertexTexCoords().ToArray();
			var normals = polygonMesh.GetFaceVertexNormals().ToArray();

			fixed ( Vector3* pNormals = &normals[0] )
				mesh.SetNormals( (IntPtr)pNormals, normals.Length );

			fixed ( Vector2* pUvs = &uvs[0] )
				mesh.SetTexCoords( (IntPtr)pUvs, uvs.Length );
		}

		var meshes_span = CollectionsMarshal.AsSpan( meshes );
		fixed ( CModelMesh* ptr = meshes_span )
		{
			var success = NativeEngine.ModelDoc.CreateModelFromMeshes( targetAbsolutePath, (IntPtr)ptr, meshes.Count );
			foreach ( var mesh in meshes )
				mesh.DeleteThis();

			if ( !success )
				return null;
		}

		var asset = AssetSystem.RegisterFile( targetAbsolutePath );
		if ( asset is null )
			return null;

		asset.Compile( true );

		return asset;
	}
}
