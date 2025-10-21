using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace BFKillFeedback
{
    public abstract class SkullBase
    {
        public bool want_destroy = false;
        public float spawn_time;
        public GameObject? game_object;
        public abstract void Update(float now_time, ModBehaviour.SkullUpdateData update_data);
        public abstract bool Create(out GameObject gameObject, out SkullBase skull);
        public abstract void Destroy();
        public abstract void DisappearRightNow();
    }
}
