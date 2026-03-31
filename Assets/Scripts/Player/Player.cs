using System;
using System.Linq;
using BlockAndDagger.Utils;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class Player : MonoBehaviour, IFocusableBlock
    {
        //reduces flicker for near-boundary cases
        private const float InspectionToleranceFromCenter = 0.8f;
        private const float InspectionExitTolerance = 0.4f;
        private const float FallingKillThresholdPosY = -20f;
        
        private CharacterController _characterController;
        public PlayerControls PlayerControls { get; private set; }
        public IBlock FocusedBlock { get; set; }
        private IBlock _inspectionBlock;
        private IBlock InspectionBlock
        {
            get { return _inspectionBlock; }
            set
            {
                _inspectionBlock = value;
#if UNITY_EDITOR
                m_debugInspectionBlock = _inspectionBlock as Block;
#endif
            }
        }
        
#if UNITY_EDITOR
        [SerializeField, ReadOnly] private Block m_debugInspectionBlock;
#endif

        public HealthStatus _hasFallen = HealthStatus.Dead;
        public int Health { get; private set; }
        public bool IsHighlighted => FocusedBlock != null;
        public bool IsNonBuild => InspectionBlock != null;
        private static IngameUI _IngameUI;
        private long _playerId;
        private float _currentBuildTime = 0;

        //ChallengeInfo settings/triggers
        private int _goalTouchCount;
        private int _currentTouchCount;
        private bool _useOnEnterCheckForGoal;
        //TODO: To prevent multiple OnTriggerEnter calls, find a better solution 
        private bool _isColliding;

        public TileType[] UnlockedBlockTypes { get; private set; } = new[]
            { TileType.Barrel, TileType.Crate, TileType.Slope, TileType.Wall, TileType.Fence };

        // Helper: calculate the logical center of a block for distance checks.
        // Prefer the block's SphereCollider center (detection collider) transformed into world space
        // if it exists. Otherwise fall back to the block's transform.position.
        private Vector3 GetBlockCenter(IBlock block)
        {
            if (block == null) return Vector3.zero;
            var mb = (block as MonoBehaviour);
            if (mb == null) return Vector3.zero;

            var sphere = mb.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                return mb.transform.TransformPoint(sphere.center);
            }

            return mb.transform.position;
        }

        private void Awake()
        {
            PlayerControls = GetComponent<PlayerControls>();
        }

        public void Init(IngameUI ingameUI)
        {
            _IngameUI = ingameUI;
        }

        public void SetChallengeSettings(ChallengeInfo challengeInfo)
        {
            _goalTouchCount = 0;
            _currentTouchCount = 0;
            _useOnEnterCheckForGoal = true;

            if (challengeInfo.challengeType == ChallengeType.GetToTarget)
            {
                _goalTouchCount = 1; //TODO: currently supports only 1 target
            }
            else if (challengeInfo.challengeType == ChallengeType.Collect)
            {
                _goalTouchCount = challengeInfo.CollectCount;
            }
            else if (challengeInfo.challengeType == ChallengeType.AgainstTime)
            {
                _useOnEnterCheckForGoal = false;
            }
        }

        public void SetAlive()
        {
            Health = 5;
            _hasFallen = HealthStatus.Alive;
        }

        private void Update()
        {
            if (_hasFallen == HealthStatus.Dead)
            {
                return;
            }

            if (transform.position.y < FallingKillThresholdPosY)
            {
                Kill("by falling");
                gameObject.SetActive(false);
            }

            _isColliding = false;
        }

        private bool _justBuildBlock;
        private IBlock _buildingBlock;
        private void KeepBuilding()
        {
            if (_buildingBlock == null)
            {
                return;
            }

            _currentBuildTime += Time.deltaTime;
            if (_currentBuildTime >= _buildingBlock.BuildTime)
            {
                _buildingBlock.SetBuildState(true);
                _currentBuildTime = 0;
                _justBuildBlock = true;
                RenderAboveTile(_buildingBlock);
                // Clear the building reference and the inspection reference if they point to the same block
                if (InspectionBlock == _buildingBlock)
                {
                    InspectionBlock = null;
                }

                _buildingBlock = null;
            }
        }

        private void FixedUpdate()
        {
            // Use the dedicated _buildingBlock so we can stop building immediately in OnTriggerExit.
            if (_buildingBlock != null && !_buildingBlock.IsBuild)
            {
                KeepBuilding();
            }
        }

        private void RenderAboveTile()
        {
            if (InspectionBlock == null)
            {  return; }

            var blockDepth = InspectionBlock.GetGridFloor();
            if (blockDepth >= BlockDepth.UpperlevelStructures)
            {
                return;
            }

            var higherBlocks = GameManager.Instance.LevelMaker.LevelData.GetGroundBlocks(blockDepth + 1);
            if (!higherBlocks.Any())
            {
                return;
            }

            //TODO: waiting the ScritableTile implementation to get rid of this junk
            var upperBlockPosY = higherBlocks.First().Data.y;
            var blockAbove = higherBlocks.FirstOrDefault(x =>
            {
                Vector3 position;
                return Math.Abs((position = x.transform.position).x - InspectionBlock.Data.x) <=
                       TileDataTracker.VectorComparisonTolerance &&
                       Math.Abs(position.y - upperBlockPosY) <= TileDataTracker.VectorComparisonTolerance &&
                       Math.Abs(position.z - InspectionBlock.Data.z) <= TileDataTracker.VectorComparisonTolerance;
            });

            if (blockAbove != null)
            {
                blockAbove.transform.gameObject.SetActive(true);
            }
        }

        private void RenderAboveTile(IBlock builtBlock)
        {
            if (builtBlock == null) return;

            var blockDepth = builtBlock.GetGridFloor();
            if (blockDepth >= BlockDepth.UpperlevelStructures)
            {
                return;
            }

            var higherBlocks = GameManager.Instance.LevelMaker.LevelData.GetGroundBlocks(blockDepth + 1);
            if (!higherBlocks.Any())
            {
                return;
            }

            var upperBlockPosY = higherBlocks.First().Data.y;
            //TODO: solve the case gracefully
            var blockAbove = (IBlock)null;
            try
            {
                blockAbove = higherBlocks.FirstOrDefault(x =>
                {
                    Vector3 position;
                    return Math.Abs((position = x.transform.position).x - builtBlock.Data.x) <=
                           TileDataTracker.VectorComparisonTolerance &&
                           Math.Abs(position.y - upperBlockPosY) <= TileDataTracker.VectorComparisonTolerance &&
                           Math.Abs(position.z - builtBlock.Data.z) <= TileDataTracker.VectorComparisonTolerance;
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error finding block above built block at ({builtBlock.Data.x}, {builtBlock.Data.y}, {builtBlock.Data.z}): {e}");
            }

            if (blockAbove != null)
            {
                blockAbove.transform.gameObject.SetActive(true);
            }
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            var isEnemy = otherCollider.transform.GetComponent<Enemy>();
            if (isEnemy != null)
            {
                Kill("by collision with Enemy");
            }

            var block = otherCollider.GetComponentInParent<IBlock>();
            if (InspectionBlock == null && block != null)
            {
                if (_isColliding)
                {
                    return;
                }

                if (block.Data.isBluePrintBlock && !block.IsBuild)
                {
                    // This prevents showing the preview when grazing the trigger from a distance
                    // and reduces flicker for near-boundary cases.
                    float dist = Vector3.Distance(transform.position, GetBlockCenter(block));
                    if (dist <= InspectionToleranceFromCenter)
                    {
                        InspectionBlock = block;
                        if (InspectionBlock is Block concreteBlock)
                        {
                            concreteBlock.SetInspectionVisual(true);
                        }
                        
                        _buildingBlock = block;
                        _currentBuildTime = 0;
                    }
                }

                CheckHitGoal(block);
            }
        }

        private void CheckHitGoal(IBlock block)
        {
            if (_useOnEnterCheckForGoal && block.IsGoal)
            {
                _currentTouchCount++;
                if (_currentTouchCount >= _goalTouchCount)
                {
                    GameManager.Instance.ProceedToNextLevel();
                }
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            _isColliding = true;

            // If we have an inspection block and the collider belongs to it, ensure building starts.
            var block = otherCollider.GetComponentInParent<IBlock>();
            if (block != null)
            {
                if (InspectionBlock != null && block == InspectionBlock)
                {
                    if (InspectionBlock.Data.isBluePrintBlock && !InspectionBlock.IsBuild)
                    {
                        if (_buildingBlock == null)
                        {
                            _buildingBlock = InspectionBlock;
                            _currentBuildTime = 0;
                        }
                    }
                }
                else
                {
                    if (InspectionBlock == null && block.Data.isBluePrintBlock && !block.IsBuild)
                    {
                        float dist = Vector3.Distance(transform.position, GetBlockCenter(block));
                        if (dist <= InspectionToleranceFromCenter)
                        {
                            InspectionBlock = block;
                            if (InspectionBlock is Block concreteBlock)
                            {
                                concreteBlock.SetInspectionVisual(true);
                            }

                            _buildingBlock = InspectionBlock;
                            _currentBuildTime = 0;
                        }
                    }
                }
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            // If we're still within the tolerance distance of the inspection block, ignore the exit
            // to avoid flicker caused by multiple trigger events at the boundary.
            if (InspectionBlock != null)
            {
                float dist = Vector3.Distance(transform.position, GetBlockCenter(InspectionBlock));
                if (dist <= InspectionExitTolerance)
                {
                    return;
                }
            }

            var exitBlock = otherCollider.GetComponentInParent<IBlock>();
            if (exitBlock != null && _buildingBlock != null && exitBlock == _buildingBlock)
            {
                if (!_buildingBlock.IsBuild)
                {
                    if (_buildingBlock is Block collapsedBlock)
                    {
                        collapsedBlock.SetInspectionVisual(false);
                    }
                }

                _buildingBlock = null;
                InspectionBlock = null;
                _currentBuildTime = 0;
                return;
            }

            if (InspectionBlock != null && InspectionBlock.Data.isBluePrintBlock)
            {
                if (!InspectionBlock.IsBuild)
                {
                    if (InspectionBlock is Block collapsedInspection)
                    {
                        collapsedInspection.SetInspectionVisual(false);
                    }

                    InspectionBlock = null;
                    _currentBuildTime = 0;
                }
                else if (_justBuildBlock)
                {
                    //TODO add these on IBlock interface
                    if (InspectionBlock.TileType == TileType.Slope)
                    {
                        InspectionBlock.transform.GetComponent<MeshCollider>().enabled = true;
                    }
                    else
                    {
                        InspectionBlock.transform.GetComponent<BoxCollider>().enabled = true;
                    }

                    _justBuildBlock = false;
                }
            }
        }

        public void Kill(string reason)
        {
            Debug.Log($"PlayerId {_playerId} died {reason}!");
            gameObject.SetActive(false);
            Health = 0;
            _hasFallen = HealthStatus.Dead;
            _buildingBlock = null;
            InspectionBlock = null;
            _currentBuildTime = 0;
            _IngameUI.ShowDeadScreen();
        }
    }
}