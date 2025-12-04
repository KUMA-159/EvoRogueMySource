/**********************************************************************/
//  説明 :
//      定数格納クラス
/**********************************************************************/


namespace Dungeon.Entities.AI
{
    static class AIData
    {
        /* byte */
        /// <summary> 要素を動かす </summary>
        public const byte ASTAR_INDEX_OFFSET = 1;
        /// <summary> マップ情報の最大要素数 </summary>
        public const byte NAVI_INDEX_LIMIT = 89;
        /// <summary> 前進乱数の最小値 </summary>
        public const byte PERCENT_RANDOM_MIN = 1;
        /// <summary> 前進乱数の最大値 </summary>
        public const byte PERCENT_RANDOM_MAX = 101;
        /// <summary> 前進乱数の最小値 </summary>
        public const byte LOOP_ERROR = 30;
        /// <summary> byteの半分値 </summary>
        public const byte HALF_BYTE = byte.MaxValue / 2;
        /// <summary> ノードがない </summary>
        public const byte NONE_NODE = 0;
    }
}