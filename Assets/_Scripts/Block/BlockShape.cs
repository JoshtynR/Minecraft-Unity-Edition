
using System;
using UnityEngine;

public class BlockShape
{
    // min 0,0,0 and max 16,16,16 is the size of a full block
    public float xMin, yMin, zMin, xMax, yMax, zMax;
    
    public BlockShape(float xMin, float yMin, float zMin, float xMax, float yMax, float zMax)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.zMin = zMin;
        this.xMax = xMax;
        this.yMax = yMax;
        this.zMax = zMax;
    }

    public void SetFaceVertices(Direction direction, Vector3Int pos, MeshData meshData)
    {
        //order of vertices matters for the normals and how we render the mesh
        // add vertices to the meshData
        
        var min = new Vector3(pos.x + xMin / 16f + 0.0005f, pos.y + yMin / 16f + 0.0005f, pos.z + zMin / 16f + 0.0005f);
        var max = new Vector3(pos.x + xMax / 16f + 0.0005f, pos.y + yMax / 16f + 0.0005f, pos.z + zMax / 16f + 0.0005f);

        //order of vertices matters for the normals and how we render the mesh
        switch (direction)
        {
            case Direction.backwards:
                meshData.AddVertex(min); // ---
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f ,pos.y + yMax/16f+0.0005f, pos.z + zMin/16f+0.0005f )); // -+-
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMax/16f+0.0005f, pos.z + zMin/16f+0.0005f )); // ++-
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMin/16f+0.0005f , pos.z + zMin/16f+0.0005f )); // +--
                break;
            case Direction.forwards:
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMin/16f+0.0005f, pos.z + zMax/16f+0.0005f )); // +-+
                meshData.AddVertex(max); // +++
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f, pos.y + yMax/16f+0.0005f, pos.z + zMax/16f+0.0005f )); // -++
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f, pos.y + yMin/16f+0.0005f, pos.z + zMax/16f+0.0005f )); // --+
                break;
            case Direction.left:
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f, pos.y + yMin/16f+0.0005f, pos.z + zMax/16f+0.0005f)); // --+
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f, pos.y + yMax/16f+0.0005f, pos.z + zMax/16f+0.0005f)); // -++
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f, pos.y + yMax/16f+0.0005f, pos.z + zMin/16f+0.0005f)); // -+-
                meshData.AddVertex(min); // ---
                break;
            case Direction.right:
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMin/16f+0.0005f, pos.z + zMin/16f+0.0005f)); // +--
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMax/16f+0.0005f, pos.z + zMin/16f+0.0005f)); // ++-
                meshData.AddVertex(max); // +++
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMin/16f+0.0005f, pos.z + zMax/16f+0.0005f)); // +-+
                break;
            case Direction.down:
                meshData.AddVertex(min); // ---
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMin/16f+0.0005f, pos.z + zMin/16f+0.0005f)); // +--
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMin/16f+0.0005f, pos.z + zMax/16f+0.0005f)); // +-+
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f, pos.y + yMin/16f+0.0005f, pos.z + zMax/16f+0.0005f)); // --+
                break;
            case Direction.up:
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f, pos.y + yMax/16f+0.0005f, pos.z + zMax/16f+0.0005f)); // -++
                meshData.AddVertex(max); // +++
                meshData.AddVertex(new Vector3(pos.x + xMax/16f+0.0005f, pos.y + yMax/16f+0.0005f, pos.z + zMin/16f+0.0005f)); // ++-
                meshData.AddVertex(new Vector3(pos.x + xMin/16f+0.0005f, pos.y + yMax/16f+0.0005f, pos.z + zMin/16f+0.0005f)); // -+-
                break;
        }
    }

    public Vector4 FaceUVTransform(Direction dir)
    {
        float uMin, uMax, vMin, vMax;
        switch (dir)
        {
            case Direction.forwards: uMin=16-xMax; uMax=16-xMin; vMin=yMin; vMax=yMax; break;
            case Direction.left: uMin=16-zMax; uMax=16-zMin; vMin=yMin; vMax=yMax; break;
            case Direction.right: uMin=zMin; uMax=zMax; vMin=yMin; vMax=yMax; break;
            case Direction.up: uMin=16-zMax; uMax=16-zMin; vMin=xMin; vMax=xMax; break;
            case Direction.down: uMin=zMin; uMax=zMax; vMin=xMin; vMax=xMax; break;
            default: uMin=xMin; uMax=xMax; vMin=yMin; vMax=yMax; break;
        }
        return new Vector4((uMax-uMin)/16f,(vMax-vMin)/16f,uMin/16f,vMin/16f);
    }

    public bool TouchesCellBoundary(Direction dir)
    {
        switch (dir)
        {
            case Direction.backwards: return zMin == 0;
            case Direction.forwards: return zMax == 16;
            case Direction.left: return xMin == 0;
            case Direction.right: return xMax == 16;
            case Direction.down: return yMin == 0;
            case Direction.up: return yMax == 16;
            default: return false;
        }
    }

    public bool CoversBoundaryFace(BlockShape other, Direction dir)
    {
        if (!TouchesCellBoundary(dir)) return false;
        switch (dir)
        {
            case Direction.left: case Direction.right:
                return yMin <= other.yMin && yMax >= other.yMax && zMin <= other.zMin && zMax >= other.zMax;
            case Direction.up: case Direction.down:
                return xMin <= other.xMin && xMax >= other.xMax && zMin <= other.zMin && zMax >= other.zMax;
            default:
                return xMin <= other.xMin && xMax >= other.xMax && yMin <= other.yMin && yMax >= other.yMax;
        }
    }

    public bool isSideFull(Direction dir)
    {
        if (!TouchesCellBoundary(dir)) return false;
        if (dir == Direction.left || dir == Direction.right) return yMin == 0 && yMax == 16 && zMin == 0 && zMax == 16;
        if (dir == Direction.up || dir == Direction.down) return xMin == 0 && xMax == 16 && zMin == 0 && zMax == 16;
        return xMin == 0 && xMax == 16 && yMin == 0 && yMax == 16;
    }

    public bool isFullBlock()
    {
        return xMin == 0 && xMax == 16 && yMin == 0 && yMax == 16 && zMin == 0 && zMax == 16;
    }
}
