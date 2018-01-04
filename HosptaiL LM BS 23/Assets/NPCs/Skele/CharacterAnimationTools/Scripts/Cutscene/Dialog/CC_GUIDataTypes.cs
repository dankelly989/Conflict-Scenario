using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
namespace GUIData
{

[Serializable]
public class NormalParagraph
{
    public string m_SpeakerName;
    public Texture m_SpeakerAvatarImg;
    public string m_Text;
}

[Serializable]
public class Options
{
    public string m_Prompt;
    public List<OneOption> m_Options;
}

[Serializable]
public class OneOption
{
    public string m_Text;
    public string m_TimeTag;
}

[Serializable]
public class MsgOptions
{
    public string m_Prompt;
    public string m_Function; //the function to call
    public List<OneMsgOption> m_Options;
}

[Serializable]
public class OneMsgOption
{
    public string m_Text;
    public string m_ExtraInfo;
}

[Serializable]
public class QTEDesc
{
    public List<QTEInput> m_RandomInputs;
    public string m_SuccessTimeTag;
    public string m_FailTimeTag;
    public float m_TimeLimit; //in second
}

[Serializable]
public class QTEInput
{
    public GUIContent m_DisplayContent;
    public KeyCode m_ExpectedInput = KeyCode.E; //one-character for Key, other input has special representations
}

}
}
