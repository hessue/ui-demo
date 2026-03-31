using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using BlockAndDagger.DebugTools;

namespace BlockAndDagger
{
    //TODO: maybe use TileBase, but requires to use of ScriptableObject(ScriptableTiles more specific)
    [Serializable]
    public class Block : MonoBehaviour, IBlock
    {
        private const float LongBuildTime = 4.0f;
        private const float BasicBuildTime = 2.0f;
        private const float BoxColliderSize = 0.9999999f;
        private readonly Vector3 _collapsedSize = new(1f, 0.01f, 1f);
        private Vector3Int _gridPos;
        private JsonBlock _data;

        public bool IsEmptyNew { get; set; }
        public JsonBlock Data => _data;
        [field: SerializeField] public bool IsDestroyable { get; private set; }
        [field: SerializeField] public TileType TileType { get; private set; }

        [field: SerializeField]
        public bool IsBluePrintBlock { get; set; } //Allows developers to mark blocks as blueprint on the Editor
        
        public bool IsBuild { get; set; }
        private float _buildTime;
        public float BuildTime => _buildTime;
        public bool IsGoal { get; set; }
        
        [field: SerializeField] public INeighbourData NeighbourData { get; private set; }
        
        public void SetNeighbours(IBlock north, IBlock east, IBlock south, IBlock west, IBlock above, IBlock under)
        {
            NeighbourData = new NeighbourData(north, east, south, west, above, under);
        }
        
        public void SetNeighbours(IBlock north, IBlock east, IBlock south, IBlock west, IBlock above, IBlock under, IBlock eastUnder, IBlock westUnder)
        {
            NeighbourData = new ExtendedNeighbourData(north, east, south, west, above, under, eastUnder, westUnder);
        }
        
        [field: SerializeField] public MetadataLabel[] MetadataField { get; private set; }

        /// The "tile" form (squeezed)
        private Material[] _unbuildMaterials;
        private Material[] _defaultMaterials;
 
        /// <summary>
        /// Created and activated ingame mode
        /// <para>BoxCollider booked for physics collision</para> 
        /// <para>BoxColliders together create the walking platform (though, the walking area should be grouped/baked somehow)</para> 
        /// </summary>
        private BoxCollider _physicsCollider;

        /// <summary>
        ///  <para>SphereCollider used to detect neighbour blueprints, size of a full block</para> 
        ///  <para>SphereCollider used ingame to detect nearby blocks, example to build a block when standing close enough</para> 
        /// </summary>
        private SphereCollider _blockDetectionCollider;

        private IBlock _blockImplementation;

        public bool CanBeBuildOn()
        {
            if (TileType == TileType.TreePine || TileType == TileType.Rocks || TileType == TileType.Flag || TileType == TileType.InvisibleWall ||
                (!IsBluePrintBlock && TileType == TileType.Fence))
            {
                return false;
            }

            return true;
        }

        //Most likely only required for LevelMaker state
        public void PopulateJsonData()
        {
            _data ??= new JsonBlock();

            var position = transform.position;
            _data.x = position.x;
            _data.y = position.y;
            _data.z = position.z;
            _data.rotationY = transform.rotation.eulerAngles.y;
            _data.type = TileType;
            _data.isBluePrintBlock = IsBluePrintBlock;
        }

        /// <summary>
        /// Example sets trigger collider size to same for each block
        /// </summary>
        public void FinalizeFreshBlockOnLevelEditor()
        {
            PopulateJsonData();
            SetBlockDetectionCollider();
        }

        public void InitBlock(JsonBlock data)
        {
            _defaultMaterials = GetComponent<MeshRenderer>().materials;
            _unbuildMaterials = new Material[_defaultMaterials.Length];
            for (int i = 0; i < _defaultMaterials.Length; i++)
            {
                _unbuildMaterials[i] = GameManager.Instance.PrefabManager.m_unbuildMaterial;
            }

            IsBuild = false;
            _data = data;
            TileType = data.type;
            if (TileType == TileType.Flag)
            {
                IsDestroyable = false;
            }

            _buildTime = GetBuildTime(TileType);
            IsBluePrintBlock = data.isBluePrintBlock;
            SetBlockDetectionCollider();

            transform.position = new Vector3(_data.x, data.y, _data.z);
            transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);
        }

        public void InitStaticBlock(JsonBlock data, Transform parent)
        {
            TileType = data.type;
            _data = data;
            transform.gameObject.isStatic = true;
            //TODO: set Static Editor Flags 

            transform.position = new Vector3(_data.x, data.y, _data.z);
            transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);

            if (parent != null)
            {
                transform.SetParent(parent);
            }
            
#if UNITY_EDITOR
            if (GameManager.Instance.DebugSettings.showStaticBlockAsPurple)
            {
                DebugUtils.ApplyPurpleTint(gameObject);
            }
#endif
        }

        private void SetBlockDetectionCollider()
        {
            //TODO: SphereCollider should be needed only for the tiles which can be build on(if no hacks in use), threes, rocks, fences do not need this
            /*if (TileType == TileType.TreePine ||  TileType == TileType.Fence || TileType == TileType.Rocks || TileType == TileType.EnemySpawner || TileType == TileType.InvisibleWall)
            {
                return;
            }*/

            _blockDetectionCollider = transform.gameObject.AddComponent<SphereCollider>();
            _blockDetectionCollider.radius = 0.5f;
            _blockDetectionCollider.center = new Vector3(0, 0.5f, 0);
            _blockDetectionCollider.isTrigger = true;
        }

        private void AddPhysicsCollider()
        {
            if (TileType == Constants.StartPositionSymbol ||
                TileType == TileType.Plant ||
                TileType == TileType.EnemySpawner ||
                TileType == TileType.AIDropDown || Data.metadata.Contains(MetadataLabel.NoColliderSecretPassage))
            {
                return;
            }
            
            if (TileType == TileType.Slope)
            {
                //Warning! defacto is to rely on _physicsCollider, in slope case this might confuse development and create bugs
                MeshCollider meshCollider = GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = gameObject.AddComponent<MeshCollider>();
                }

                meshCollider.convex = true;
                meshCollider.enabled = true;
                _physicsCollider = null; //not used in slope
            }
            else
            {
                if (_physicsCollider == null)
                {
                    _physicsCollider = gameObject.AddComponent<BoxCollider>();
                }

                _physicsCollider.size.Set(BoxColliderSize, BoxColliderSize, BoxColliderSize);
                _physicsCollider.enabled = true;
            }
        }

        private float GetBuildTime(TileType tileType)
        {
            float buildTime;
            switch (tileType)
            {
                case TileType.Slope:
                case TileType.Wall:
                case TileType.Plant:
                    buildTime = LongBuildTime;
                    break;

                case TileType.Crate:
                case TileType.Barrel:
                case TileType.Fence:
                    buildTime = BasicBuildTime;
                    break;
                default:
                    buildTime = 999999;
                    break;
            }

            return buildTime;
        }

        /// <summary>
        /// Enables colliders, sets scale etc
        /// </summary>
        public void SetPlayModeSettings()
        {
            AddPhysicsCollider();
            SetBuildState(false);

            //Will be hidden until block under neat gets build
            if (GetGridFloor() == BlockDepth.UpperlevelStructures && IsBluePrintBlock)
            {
                transform.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Enables colliders, sets scale etc
        /// </summary>
        public void SetLevelBuilderModeSettings()
        {
            if (!IsBluePrintBlock)
            {
                EnableGoRenderers(true);
            }
        }

        private void EnableGoRenderers(bool enable)
        {
            if (TileType == TileType.InvisibleWall || TileType == TileType.EnemySpawner)
            {
                var spawnRenderers = GetComponentsInChildren<MeshRenderer>();
                spawnRenderers.ToList().ForEach(x => x.enabled = enable); //Destroying would be better, this is fine for now
            }
        }

        public void SetBuildState(bool completed)
        {
            if (!IsBluePrintBlock)
            {
                EnableGoRenderers(false);
                return;
            }

            //TODO: Blueprints will be probably Navmesh Obstacles
            int excludeFromNavMeshBaking = LayerMask.NameToLayer("NavMesh Obstacle");
            gameObject.layer = excludeFromNavMeshBaking;

            if (TileType == TileType.Slope)
            {
                var meshCollider = gameObject.GetComponent<MeshCollider>();
                if (completed)
                {
                    GetComponent<MeshRenderer>().materials = _defaultMaterials;
                    transform.localScale = Vector3.one; //TODO: use renderer instead if possible
                    IsBuild = true;
                    meshCollider.enabled = true;
                }
                else
                {
                    GetComponent<MeshRenderer>().materials = _unbuildMaterials;
                    transform.localScale = _collapsedSize;
                    meshCollider.enabled = false; //Enabled when tile is build, check Player.cs
                    IsBuild = false;
                }
            }
            else
            {
                var center = _blockDetectionCollider.center;
                if (completed)
                {
                    GetComponent<MeshRenderer>().materials = _defaultMaterials;
                    _physicsCollider.enabled = true;
                    transform.localScale = Vector3.one;
                    _blockDetectionCollider.center = new Vector3(center.x, 0.5f, center.z);
                    IsBuild = true;
                }
                else
                {
                    GetComponent<MeshRenderer>().materials = _unbuildMaterials;
                    _physicsCollider.enabled = false;
                    transform.localScale = _collapsedSize;
                    _blockDetectionCollider.center = new Vector3(center.x, 50f, center.z);
                    IsBuild = false;
                }

                //The new NavMeshObstacle way:
                if (completed)
                {
                    //TODO: attach this at the block initialization phase
                    gameObject.AddComponent(typeof(NavMeshObstacle));
                }
            }

            //TODO: READ https://docs.unity3d.com/ScriptReference/AI.NavMeshObstacle.html
            // How does this scale, how does the baking(carving surface) affect performance,
            // how small NavMeshSurfaces the level needs to be split to
            GameManager.Instance.LevelMaker.m_activeLevel.BuildNavMeshSurface();
        }

        public void SetInspectionVisual(bool show)
        {
            if (!IsBluePrintBlock)
                return;

            if (_blockDetectionCollider == null)
            {
                SetBlockDetectionCollider();
            }

            // Slope handled similarly but keep simple scaling behavior
            if (TileType == TileType.Slope)
            {
                transform.localScale = show ? Vector3.one : _collapsedSize;
            }
            else
            {
                transform.localScale = show ? Vector3.one : _collapsedSize;
            }
        }

        public void Highlight() //TODO: Replace debug gizmo version with real one
        {
            //TODO: Outline or material change?
        }
        
        /// <summary>
        /// DOES NOT SUPPORT EMPTY BLOCK!
        /// </summary>
        public BlockDepth GetGridFloor()
        {
            return Enum.Parse<BlockDepth>(gameObject.transform.parent.name);
        }
    }
}
