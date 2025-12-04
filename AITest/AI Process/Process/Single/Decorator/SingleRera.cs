/********************************************************************/
//  説明 :
//      Singleのレアエネミー用の動作
/********************************************************************/

using UnityEngine;

namespace Dungeon.Entities.AI
{
    public class SingleRera : AIDecorator
    {
        // 短縮
        private EntityMovement Movement => enemy_entity_.Movement;
        private Vector3Int EnemyPosition => enemy_entity_.MapPosition;
        private Vector3Int PlayerPosition => player_entity_.MapPosition;





        public SingleRera(AIStetaCall component, AIEntity Entitys) : base(component)
        {
            init_position_ = Entitys.enemy_.MapPosition;
            enemy_entity_ = Entitys.enemy_;
            player_entity_ = Entitys.player_;
        }


        protected internal override void CallNoop()
        {
            //call_.CallNoop();
        }


        protected internal override void CallRoaming()
        {
            call_.CallRoaming();
        }


        protected internal override void CallTracking()
        {
            //call_.CallTracking();
        }


        protected internal override void CallAreaChange()
        {
            call_.CallAreaChange();
        }


        protected internal override void CallSleep()
        {
            //call_.CallSleep();
        }


        protected internal override void CallEscape()
        {
            //IS_DEBUG
            //if (DebugDistance(EnemyPosition, PlayerPosition) == false) return;



            AIMoveAction Action;       // Action
            Direction8 my_direction;   // 自身の向いている角度
            Vector3Int direction;      // プレイヤーへの角度
            float degree;              // プレイヤーへの角度
            byte rand_action;          // 4分の1の確率で行動しない
            bool is_direction;         // 移動許可



            // プレイヤー方向の角度を求める
            direction = player_entity_.MapPosition - enemy_entity_.MapPosition;
            degree = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Action = new AIMoveAction();

            rand_action = (byte)Random.Range(1, 5);
            is_direction = false;

            // １以外であれば行動
            if (rand_action != 1)
            {
                // 反転
                degree += 180;


                // ±45度でチェックする。
                my_direction = DirectionUtilties.GetNearDirection(degree);
                if (is_direction == false)
                {
                    is_direction = Action.GetAdjacentCell(my_direction, enemy_entity_.MapPosition) != sbyte.MaxValue;
                }

                if (is_direction == false)
                {
                    my_direction = DirectionUtilties.GetNearDirection(degree + 45.0F);
                    is_direction = Action.GetAdjacentCell(my_direction, enemy_entity_.MapPosition) != sbyte.MaxValue;
                }

                if (is_direction == false)
                {
                    my_direction = DirectionUtilties.GetNearDirection(degree - 45.0F);
                    is_direction = Action.GetAdjacentCell(my_direction, enemy_entity_.MapPosition) != sbyte.MaxValue;
                }

                // 移動
                enemy_entity_.ChangeEntityDirection(my_direction);
                enemy_entity_.Movement.MoveTo(Direction8.FORWARD);
            }
        }
    }
}