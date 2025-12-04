/********************************************************************/
//  説明 :
//      現在のAIの状態を変化させる
/// MEMO : Sleepの処理が足りない
/********************************************************************/

using Dungeon.Entities.Enemy;
using Dungeon.Player;
using UnityEngine;


namespace Dungeon.Entities.AI
{
    public class AIBrain
    {
        /// <summary> Enemy情報 </summary>
        private readonly EnemyEntity enemy_entity_;
        /// <summary> player情報 </summary>
        private readonly PlayerEntity player_pntity_;


        /// <summary> area_change_count_最大値 </summary>
        private const byte AREA_CHANGE_MAX = 20;
        /// <summary> area_change_count_最小値 </summary>
        private const byte AREA_CHANGE_MIN = 10;


        /// <summary> エリア変更カウント </summary>
        private byte area_change_count_;
        /// <summary> エリア変更上限     </summary>
        private byte area_change_limit_;
        /// <summary> 寝る可能性 </summary>
        private byte sleep_percent_;
        /// <summary> Sleepカウント </summary>
        private byte sleep_count_;
        /// <summary> 何もしない可能性 </summary>
        private byte noop_percent_;
        /// <summary> Sleep状態 </summary>
        private bool now_sleep_;


        /// <summary> AIの状態 </summary>
        public AIstate AiState { get; private set; }

        /// <summary> AIの種類 </summary>
        public AItype AiType { get; }



        private AIBrain() { }

        /// <summary> Entity情報を格納する </summary>
        /// <param name="Entitys"> エンティティ統括 </param>
        /// <param name="Type">    AIの種類 </param>
        /// <param name="Sleep">   眠る確率 </param>
        /// <param name="Noop">    何もしない確率 </param>
        public AIBrain(AIEntity Entitys, AItype Type, byte Sleep, byte Noop)
        {
            // エンティティ初期化
            enemy_entity_ = Entitys.enemy_;
            player_pntity_ = Entitys.player_;

            // メンバ初期化
            AiType = Type;
            AiState = AIstate.Roaming;
            area_change_count_ = 0;
            sleep_count_ = 0;
            sleep_percent_ = Sleep;
            noop_percent_ = Noop;
            now_sleep_ = false;

            // 乱数格納
            area_change_limit_ = (byte)Random.Range(AREA_CHANGE_MIN, AREA_CHANGE_MAX);
        }


        /// <summary> AIStateを変化させる </summary>
        /// <param name="SearchLength">   索敵距離 </param>
        /// <param name="SearchRange">    索敵範囲 </param>
        /// <param name="DetectionRange"> 探知距離 </param>
        public void AIStateConversion(byte SearchLength, float SearchRange, byte DetectionRange)
        {
            // 優先度が低いものからcallする。
            // デフォルトは索敵。
            AiState = AIstate.Roaming;


            ChengeSleep();

            //寝ているときは判定を行わない
            if (now_sleep_ == false)
            {
                ChengeNoop();
                ChengeAreaChange();
                ChengeTracking(SearchLength, SearchRange, DetectionRange);
            }

            // Rareのみ追加処理を行う
            if (AiType == AItype.Rare)
            {
                if (PlayerSearch(SearchLength, 360.0F) == true)
                {
                    AiState = AIstate.Escape;
                }
            }

            // エリアチェンジのためのカウントアップ
            if (AiState == AIstate.Roaming)
            {
                area_change_count_++;
            }
        }


        /// <summary> AreaChange不成立に変更 </summary>
        public void EndAreaChange()
        {
            area_change_count_ = 0;
            area_change_limit_ = (byte)Random.Range(AREA_CHANGE_MIN, AREA_CHANGE_MAX);
        }


        /// <summary> Roamingに変更 </summary>
        public void SetRoaming()
        {
            AiState = AIstate.Roaming;
        }



        /// <summary> Playerの索敵 </summary>
        /// <param name="SearchLength"> 索敵距離 </param>
        /// <param name="SearchRange">  索敵範囲 </param>
        private bool PlayerSearch(byte SearchLength, float SearchRange)
        {
            /* Vector3 */
            Vector3 fan_to_point;   // Mobから見たプレイヤーの方向

            /* float */
            float search_length;   // 索敵距離の長さ
            float search_dot;      // 内積化された索敵範囲
            float search_cos;      // cos化された索敵角度



            // 敵から見たプレイヤーの方向の取得
            fan_to_point = (Vector3)player_pntity_.MapPosition - (Vector3)enemy_entity_.MapPosition;
            // 索敵距離の長さを取得
            search_length = Mathf.Sqrt((fan_to_point.x * fan_to_point.x) + (fan_to_point.z * fan_to_point.z));


            // 索敵距離内か判定
            if (SearchLength > search_length)
            {
                // 索敵角度をcosに変換
                search_cos = Mathf.Cos(SearchRange / 2.0F * Mathf.Rad2Deg);

                // 内積計算
                search_dot = Vector3.Dot(enemy_entity_.transform.forward, fan_to_point.normalized);


                // 索敵範囲内
                if (search_dot > search_cos == true)
                {
                    // 発見
                    return true;
                }
            }

            // 未発見
            return false;
        }


        /// <summary>                     Tracking変化処理 </summary>
        /// <param name="SearchLength">   索敵距離 </param>
        /// <param name="SearchRange">    索敵範囲 </param>
        /// <param name="DetectionRange"> 探知距離 </param>
        private void ChengeTracking(byte SearchLength, float SearchRange, byte DetectionRange)
        {
            if (PlayerSearch(DetectionRange, 360.0F) == true || PlayerSearch(SearchLength, SearchRange) == true)
            {
                AiState = AIstate.Tracking;
            }
        }


        /// <summary> AreaChange変化処理 </summary>
        private void ChengeAreaChange()
        {
            // 上限値以上の場合に変化
            if (area_change_count_ >= area_change_limit_)
            {
                AiState = AIstate.AreaChange;
            }
        }


        /// <summary> Sleep変化処理 </summary>
        private void ChengeSleep()
        {
            /* byte */
            const byte SLEEP_TURN = 3;   // エリア移動終了までのカウント
            byte randam_sleep;   // 眠る確率



            randam_sleep = (byte)Random.Range(AIData.PERCENT_RANDOM_MIN, AIData.PERCENT_RANDOM_MAX);

            // 確率で眠る
            if (randam_sleep <= sleep_percent_)
            {
                now_sleep_ = true;
            }

            // Sleep終了判定
            if (sleep_count_ >= SLEEP_TURN)
            {
                now_sleep_ = false;
                sleep_count_ = 0;
            }

            // Sleepカウント
            if (now_sleep_ == true)
            {
                sleep_count_++;
                AiState = AIstate.Sleep;
            }
        }


        /// <summary> Noop変化処理 </summary>
        private void ChengeNoop()
        {
            /* byte */
            byte randam_noop;   // 何もしない確率



            randam_noop = (byte)Random.Range(AIData.PERCENT_RANDOM_MIN, AIData.PERCENT_RANDOM_MAX);

            // 確率で行動をしない
            if (randam_noop <= noop_percent_)
            {
                AiState = AIstate.Noop;
            }
        }
    }
}