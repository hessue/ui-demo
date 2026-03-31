using System;

namespace BlockAndDagger
{
    [Serializable]
    public class JsonBlock
    {
        public bool isStaticGameObject;
        public TileType type;
        public float x;
        public float y;
        public float z;
        public float rotationY;
        public int hp;
        
        public MetadataLabel [] metadata = Array.Empty<MetadataLabel>(); //Used for more complex blocks, like spawners, etc.

        //Player can only replace/build on BlueprintBlocks with rare exceptions(Flag block)
        public bool isBluePrintBlock;
    }

    public enum MetadataLabel
    {
        SpawnHighGroundEnemyType,
        NoColliderSecretPassage,
    }
}
