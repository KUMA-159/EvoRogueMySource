using Dungeon.Entities.Enemy;
using Dungeon.Player;
using Sirenix.OdinInspector;
using System;
using UnityEngine;


namespace Dungeon.Entities.AI
{
    /// <summary> AIの状態 </summary>
    public enum AIstate : byte
    {
        /// <summary> 待機 </summary>
        Noop,
        /// <summary> 索敵 </summary>
        Roaming,
        /// <summary> 追跡 </summary>
        Tracking,
        /// <summary> エリア変更 </summary>
        AreaChange,
        /// <summary> 睡眠 </summary>
        Sleep,
        /// <summary> 逃走 </summary>
        Escape,
        /// <summary> 最大値 </summary>
        Max
    };



    /// <summary> AIの種類 </summary>
    public enum AItype : byte
    {
        /// <summary> ノーマル </summary>
        Normal,
        /// <summary> レア </summary>
        Rare,
        /// <summary> 飛行 </summary>
        Fly,
        /// <summary> 潜水 </summary>
        Dive,
        /// <summary> 壁移動 </summary>
        Spider
    };



    /// <summary> Entity譲渡用クラス </summary>
    public class AIEntity
    {
        public EnemyEntity enemy_;
        public PlayerEntity player_;
        public byte search_length_;
    }



    [CreateAssetMenu(menuName = "Dungeon/AIModule/NormalAI")]
    public sealed class EvoRogueAI : EnemyAIModule
    {
        /// <summary> 視野角 </summary>
        private const float FOV = 360.0F;
        /// <summary> 確率</summary>
        private const byte PROBABILITY = 100;


        /// <summary> 状態に対応した処理を呼ぶ </summary>
        private AIStetaCall is_call_;
        /// <summary> AIの状態を変化させる </summary>
        private AIBrain chenge_;

        public override bool IsMultiInstance => true;

        /// <summary> Sleep再生オブジェクト </summary>
        [LabelText("Sleepパーティカル")]
        public GameObject sleep_object_;

        /// <summary> Sleep再生オブジェクト </summary>
        [LabelText("Sleepマテリアル")]
        public Material sleep_material_;

        /// <summary> AI種類 </summary>
        [LabelText("AIType")]
        public AItype ai_type_;

        /// <summary> 索敵範囲 </summary>
        [LabelText("索敵範囲"), Range(0.0F, FOV)]
        public float search_range_;

        /// <summary> 索敵距離 </summary>
        [LabelText("索敵距離")]
        public byte search_length_;

        /// <summary> 探知距離 </summary>
        [LabelText("探知距離")]
        public byte detection_range_;

        /// <summary> 前に進み続ける確率 </summary>
        [LabelText("前に進み続ける確率"), Range(0, PROBABILITY)]
        [ShowIf("@ai_type_ == AItype.Normal || ai_type_ == AItype.Spider")]
        [EnableIf("@ai_type_ == AItype.Normal || ai_type_ == AItype.Spider")]
        public byte forward_percent_;

        /// <summary> 眠る確率 </summary>
        [LabelText("眠る確率"), Range(0, PROBABILITY)]
        [ShowIf("@ai_type_ == AItype.Normal || ai_type_ == AItype.Spider")]
        [EnableIf("@ai_type_ == AItype.Normal || ai_type_ == AItype.Spider")]
        public byte sleep_percent_;

        /// <summary> 何もしない確率 </summary>
        [LabelText("何もしない確率"), Range(0, PROBABILITY)]
        [ShowIf("@ai_type_ == AItype.Normal || ai_type_ == AItype.Spider")]
        [EnableIf("@ai_type_ == AItype.Normal || ai_type_ == AItype.Spider")]
        public byte noop_percent_;

        /// <summary> AIstate.Maxで通常呼び出し,それ以外で固定呼び出し。 </summary>
        [SerializeField]
        [LabelText("DEBUG用state"), Title("DEBUG")]
        [InfoBox("OFF")]
        private AIstate debug_state_;

        /// <summary> ONで呼び出し毎にカメラの位置にテレポートする </summary>
        [SerializeField]
        [LabelText("テレポートスイッチ")]
        [InfoBox("OFF")]
        private bool debug_is_teleport_;





        public override void OnStart(EnemyEntity e)
        {
            AIEntity entitys;



            // AIに渡すために格納
            entitys = new AIEntity();
            entitys.enemy_ = e;
            entitys.player_ = Player;
            entitys.search_length_ = search_length_;

            chenge_ = new AIBrain(entitys, ai_type_, sleep_percent_, noop_percent_);


            // AItypeで動きを変える
            switch (ai_type_)
            {
                // 特殊なAI。Trakingの処理が消えている。
                case AItype.Rare:
                    is_call_ = new SingleRera(new SingleAIProcess(entitys, forward_percent_), entitys);


                    break;

                case AItype.Fly:
                    is_call_ = new FlywayAIProcess(entitys);

                    e.ChangeMovement(MoveMobility.FLYWAY);

                    break;

                case AItype.Spider:
                    is_call_ = new SpiderAIProcess(entitys, forward_percent_);

                    e.ChangeMovement(MoveMobility.CRAWL);

                    break;

                // dafaultはNormalとして扱う
                default:
                    is_call_ = new SingleAIProcess(entitys, forward_percent_);

                    e.ChangeMovement(MoveMobility.GROUND);

                    break;
            }
        }


        public override void SelectAction(EnemyEntity e)
        {
            // 死んだ場合動かない
            if (e.IsLiving == false) return;

            // Entity更新（ジャンプ力などが途中で変わることがあるため）
            is_call_.UpdateEntity(e);

            if (e.UseSkill(EnemySkillType.NORMAL_ATTACK) == false)
            {
                // AIの状態を決定する。
                chenge_.AIStateConversion(search_length_, search_range_, detection_range_);
            }



            //IS_DEBUG
            //// カメラの位置にテレポートする。
            //if (debug_is_teleport_ == true)
            //{
            //    int x = (int)Camera.main.transform.position.x;
            //    int y = (int)Camera.main.transform.position.y;
            //    int z = (int)Camera.main.transform.position.z;

            //    Player.TeleportTo(new Vector3Int(x, y, z));
            //}

            //// AIstate.Maxで通常呼び出し。
            //// それ以外で固定呼び出し。
            //if (debug_state_ == AIstate.Max)
            //{
            //    is_call_.AIStateCall(chenge_.AiState);
            //    DLog.Log(chenge_.AiState, 33);
            //}
            //else
            //{
            //    is_call_.AIStateCall(debug_state_);
            //}



            // AIの処理を呼ぶ
            is_call_.AIStateCall(chenge_.AiState);

            // 条件を満たした場合にAreaChangeを呼ぶのをやめる。
            if (is_call_.EndAreaChange == true)
            {
                chenge_.EndAreaChange();
            }
            // スタック防止処理
            if (is_call_.IsRoaming == true)
            {
                chenge_.SetRoaming();
            }

        }
    }
}