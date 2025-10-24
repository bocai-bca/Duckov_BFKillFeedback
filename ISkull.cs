using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEngine;

namespace BFKillFeedback
{
	public interface ISkull
	{
		public bool WantDestroy
		{
			get; set;
		}
		public float SpawnTime
		{
			get; set;
		}
		public GameObject? TheGameObject
		{
			get; set;
		}
		public float EffectWidth
		{
			get; set;
		}
		public void UpdateAlpha(float now_time);
		public void UpdatePosition(float now_time, float total_width, int index);
		public void Destroy();
		public static bool Create(out GameObject gameObject, out ISkull skull) => throw new NotImplementedException();
		public void DisappearRightNow();
	}
}
