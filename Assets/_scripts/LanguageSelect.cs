using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AzureOpenAIController;

public class LanguageSelect : MonoBehaviour
{
    public LangObj lang;
    public static Action<LangObj> OnLangSelected;
    public class LangObj {
        public string recognitionLanguage;
        public string synthesisLanguage;
        public string synthesisVoiceName;
        public string ChineseName;
        public bool isDefault;
    }
    
    // Start is called before the first frame update
    void Start()
    {
/*        OnDialectFuncRecieve += SelectLanguage;*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public LangObj LangSichuan() {
        LangObj langObj = new LangObj();
        langObj.recognitionLanguage = "zh-CN";
        langObj.synthesisLanguage = "zh-CN-sichuan";
        langObj.synthesisVoiceName = "zh-CN-sichuan-YunxiNeural";
        langObj.ChineseName = "四川";
        return langObj;
    }

    public LangObj LangPutong()
    {
        LangObj langObj = new LangObj();
        langObj.recognitionLanguage = "zh-CN";
        langObj.synthesisLanguage = "zh-CN";
        langObj.synthesisVoiceName = "zh-CN-XiaochenNeural";
        langObj.ChineseName = "普通";
        return langObj;
    }

    public LangObj LangGuangxi()
    {
        LangObj langObj = new LangObj();
        langObj.recognitionLanguage = "zh-CN";
        langObj.synthesisLanguage = "zh-CN-GUANGXI";
        langObj.synthesisVoiceName = "zh-CN-guangxi-YunqiNeural";
        langObj.ChineseName = "广西";
        return langObj;
    }

    public LangObj LangDongbei()
    {
        LangObj langObj = new LangObj();
        langObj.recognitionLanguage = "zh-CN";
        langObj.synthesisLanguage = "zh-CN-liaoning";
        langObj.synthesisVoiceName = "zh-CN-liaoning-XiaobeiNeural";
        langObj.ChineseName = "东北";
        return langObj;
    }

    public LangObj LangShanxi()
    {
        LangObj langObj = new LangObj();
        langObj.recognitionLanguage = "zh-CN";
        langObj.synthesisLanguage = "zh-CN-shaanxi";
        langObj.synthesisVoiceName = "zh-CN-shaanxi-XiaoniNeural";
        langObj.ChineseName = "陕西";
        return langObj;
    }

    public LangObj LangGuangdong()
    {
        LangObj langObj = new LangObj();
        langObj.recognitionLanguage = "zh-CN";
        langObj.synthesisLanguage = "zh-HK";
        langObj.synthesisVoiceName = "zh-HK-HiuMaanNeural";
        langObj.ChineseName = "广东";
        return langObj;
    }

    public LangObj LangTaiwan()
    {
        LangObj langObj = new LangObj();
        langObj.recognitionLanguage = "zh-CN";
        langObj.synthesisLanguage = "zh-TW";
        langObj.synthesisVoiceName = "zh-TW-HsiaoChenNeural";
        langObj.ChineseName = "台湾";
        return langObj;
    }

   /* public void SelectLanguage(FunctionCallResponse language) {
        switch (language.arguments.p1)
        {
            case "guangdong":
                // Code to execute if state is 1
                Debug.Log("State is guangdong");
                lang = LangGuangdong();
                break;
            case "shaanxi":
                // Code to execute if state is 1
                Debug.Log("State is shaanxi");
                lang = LangShanxi();
                break;
            case "taiwan":
                // Code to execute if state is 1
                Debug.Log("State is taiwan");
                lang = LangTaiwan();
                break;
            case "dongbei":
                // Code to execute if state is 1
                Debug.Log("State is dongbeo");
                lang = LangDongbei();
                break;
            case "putong":
                // Code to execute if state is 1
                Debug.Log("State is putong");
                lang = LangPutong();
                break;
            default:
                // Code to execute if state is 1
                Debug.Log("State is dongbeo");
                lang = LangDongbei();
                break;
        }
        OnLangSelected?.Invoke(lang);
    }*/

}
