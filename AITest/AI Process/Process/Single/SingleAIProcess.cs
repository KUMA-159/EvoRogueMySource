using UnityEngine;


namespace Dungeon.Entities.AI
{
    public sealed class SingleAIProcess : AIStetaCall
    {
        /// <summary> 移動処理 </summary>
        private SingleAIMoveAction move_action_;
        /// <summary> 移動処理 </summary>
        private byte search_length_;

        // 短縮
        private EntityMovement Movement => enemy_entity_.Movement;
        private Vector3Int EnemyPosition => enemy_entity_.MapPosition;
        private Vector3Int PlayerPosition => player_entity_.MapPosition;





        public SingleAIProcess(AIEntity Entitys, byte ForwardPercent)
        {
            // エンティティ初期化
            init_position_ = Entitys.enemy_.MapPosition;
            enemy_entity_ = Entitys.enemy_;
            player_entity_ = Entitys.player_;
            search_length_ = Entitys.search_length_;

            EndAreaChange = false;
            IsRoaming = false;

            // 移動処理
            move_action_ = new SingleAIMoveAction(enemy_entity_, ForwardPercent);
        }


        /// <summary> Roaming処理を呼ぶ </summary>
        protected internal override void CallRoaming()
        {
            //IS_DEBUG
            if (DebugDistance(EnemyPosition, PlayerPosition) == false) return;



            // 移動先を決める
            move_action_.RandomMove();

            // 移動要求
            Movement.MoveTo(move_action_.DestinationFace, MoveType.HORIZONTAL, true);

            // 座標初期化
            if (move_action_.InitEntity == true)
            {
                SetInitEntity();
            }
        }


        /// <summary> Tracking処理を呼ぶ </summary>
        protected internal override void CallTracking()
        {
            //IS_DEBUG
            if (DebugDistance(EnemyPosition, PlayerPosition) == false) return;



            // 0以下かつで移動できないのならA*再帰計算
            if ((PlayerPosition.y - EnemyPosition.y) <= 0)
            {
                move_action_.AstarCalculation(EnemyPosition, PlayerPosition);
                move_action_.NodeClear();
            }
            else
            {
                move_action_.AreaChange(PlayerPosition, (byte)(search_length_ + 1));
            }

            // AIの状態を変化させる
            IsRoaming = move_action_.StackOut(EnemyPosition, move_action_.Destination);

            // プレイヤーの方向を向く
            enemy_entity_.LookToPosition(move_action_.Destination);
            enemy_entity_.Movement.MoveTo(Direction8.FORWARD);
            DLog.Log(move_action_.DestinationFace, 36);
        }


        /// <summary> AreaChange処理を呼ぶ </summary>
        protected internal override void CallAreaChange()
        {
            //IS_DEBUG
            if (DebugDistance(EnemyPosition, PlayerPosition) == false) return;



            // エリアチェンジ
            move_action_.AreaChange(EnemyPosition);
            EndAreaChange = move_action_.IsAreaChange;

            enemy_entity_.LookToPosition(move_action_.Destination);
            enemy_entity_.Movement.MoveTo(Direction8.FORWARD);
        }





        private SingleAIProcess() { }

        /// <summary> 
        /// Wait処理を呼ぶ
        /// <para> 処理なし </para>
        /// </summary>
        protected internal override void CallNoop() { }

        /// <summary> 
        /// Sleep処理を呼ぶ
        /// <para> 処理なし </para>
        /// </summary>
        protected internal override void CallSleep() { }

        /// <summary> 
        /// Escape処理を呼ぶ
        /// <para> 処理なし </para>
        /// </summary>
        protected internal override void CallEscape() { }
    }
}