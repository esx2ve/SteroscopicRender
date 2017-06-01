using UnityEditor;
using System;

//-----------------------------------------------------------------------------
// Copyright 2015-2017 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo.Editor
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(AudioOutput))]
	public class AudioOutputEditor : UnityEditor.Editor
	{
		private AudioOutput _target;
		private SerializedProperty _channelMaskProperty;
		private string[] _channelMaskOptions = {"1", "2" , "3" , "4" , "5" , "6" , "7" , "8"};

		void OnEnable()
		{
			_target = (this.target) as AudioOutput;
			_channelMaskProperty = serializedObject.FindProperty("_channelMask");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawDefaultInspector();

			if(_target._audioOutputMode == AudioOutput.AudioOutputMode.Multiple)
			{
				_channelMaskProperty.intValue = EditorGUILayout.MaskField("Channels", _channelMaskProperty.intValue, _channelMaskOptions);
			}
			else
			{
				int prevVal = 0;
				for(int i = 1; i <= 8; ++i)
				{
					if((_channelMaskProperty.intValue & (1 << (i - 1))) > 0)
					{
						prevVal = i;
						break;
					}
				}

				int newVal = Math.Min(Math.Max(EditorGUILayout.IntSlider("Channel", prevVal, 0, 8), 0), 8);
				_channelMaskProperty.intValue = newVal == 0 ? 0 : 1 << (newVal - 1);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
