/**********************************************************************/
//  説明 :
//      Decorator基底クラス
/**********************************************************************/


namespace Dungeon.Entities.AI
{
    public abstract class AIDecorator : AIStetaCall
    {
        protected AIStetaCall call_;

        public AIDecorator(AIStetaCall Call)
        {
            this.call_ = Call;
        }
    }
}


/// MOME: Decoratorで実験してみたクラス。
/// あとで追加実装がある際に使う
/// callする際のやり方。
/// 
/// is_call_ = new DogDecorator(new SingleAIProcess(e, Player, forward_percent_));