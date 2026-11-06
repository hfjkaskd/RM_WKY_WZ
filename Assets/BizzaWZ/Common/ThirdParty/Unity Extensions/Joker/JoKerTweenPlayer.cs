using System.Collections;
using System.Collections.Generic;

namespace UnityExtensions.Tween
{
    public partial class TweenPlayer : ConfigurableUpdateComponent
    {
        public void ClearAnimation()
        {
            _animations?.Clear();
        }

        public void SetAnimations(List<TweenAnimation> anims)
        {
            _animations = anims;
        }
    }
}
