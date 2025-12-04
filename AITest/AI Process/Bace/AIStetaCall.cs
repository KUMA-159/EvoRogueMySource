/**********************************************************************/
//  説明 :
//      AI処理callの基底クラス
/**********************************************************************/

using Dungeon.Entities.Enemy;
using Dungeon.Player;
using UnityEngine;


namespace Dungeon.Entities.AI
{
    public abstract class AIStetaCall
    {
        /// <summary> AIのcall関数を格納 </summary>
        private delegate void EnterAIStateCall();


        /// <summary> Enemyエンティティ </summary>
        protected EnemyEntity enemy_entity_;
        /// <summary> playerエンティティ </summary>
        protected PlayerEntity player_entity_;
        /// <summary> 初期座標 </summary>
        protected Vector3Int init_position_;


        /// <summary> AreaChange終了条件 </summary>
        public bool EndAreaChange { get; protected set; }
        /// <summary> Roamingステートに遷移する条件 </summary>
        public bool IsRoaming { get; protected set; }


        // AI処理の中身
        protected internal abstract void CallNoop();
        protected internal abstract void CallRoaming();
        protected internal abstract void CallAreaChange();
        protected internal abstract void CallTracking();
        protected internal abstract void CallSleep();
        protected internal abstract void CallEscape();



        /// <summary> 初期状態に変更 </summary>
        /// <param name="Face"> 終点 </param>
        protected void SetInitEntity(EvoDirection Face = EvoDirection.DOWN)
        {
            DLog.Log("Error：バグ回避のためにテレポート", 34);

            // 初期状態に変更
            enemy_entity_.ChangeEntityGround(Face);
            enemy_entity_.TeleportTo(init_position_);
        }


        /// <summary> Entity更新 </summary>
        /// <param name="E"> EnemyEntity情報 </param>
        public void UpdateEntity(EnemyEntity E)
        {
            enemy_entity_ = E;
        }


        /// <summary> AI処理を呼ぶ </summary>
        /// <param name="State"> AIの状態 </param>
        public void AIStateCall(AIstate State)
        {
            EnterAIStateCall[] call = new EnterAIStateCall[(int)AIstate.Max];   // 関数格納



            // 処理格納
            call[(int)AIstate.Noop] = CallNoop;
            call[(int)AIstate.Roaming] = CallRoaming;
            call[(int)AIstate.Tracking] = CallTracking;
            call[(int)AIstate.AreaChange] = CallAreaChange;
            call[(int)AIstate.Sleep] = CallSleep;
            call[(int)AIstate.Escape] = CallEscape;

            call[(int)State]();
        }


        /// <summary> 
        /// デバック用
        /// <para> プレイヤーから近い敵キャラのみを動かす </para>
        /// </summary>
        protected bool DebugDistance(Vector3Int Point1, Vector3Int Point2)
        {
            Vector3Int calculation;   // 計算用Vector



            calculation = new Vector3Int();

            calculation.x = Mathf.Abs(Point1.x - Point2.x);
            calculation.y = Mathf.Abs(Point1.y - Point2.y);
            calculation.z = Mathf.Abs(Point1.z - Point2.z);

            return (calculation.x + calculation.y + calculation.z) <= 15;
        }
    }
}