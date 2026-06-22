using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PuppetBuilder - 代码自动生成木偶角色模型
/// 
/// 功能：
/// 1. 用 Unity 原生几何体（Cube + Cylinder + Sphere）拼出木偶形状
/// 2. 支持男性/女性两种体型
/// 3. 自动添加关节、材质、碰撞体
/// 4. 零外部模型依赖，纯代码生成
/// 
/// 使用方法：
/// 1. 创建空物体，挂载此脚本
/// 2. 设置 puppetType 为 Male 或 Female
/// 3. 点击 Play 自动生成完整木偶
/// 4. 生成后保存为预制体即可复用
/// </summary>
public class PuppetBuilder : MonoBehaviour
{
    public enum PuppetType { Male, Female }

    [Header("木偶类型")]
    [SerializeField] private PuppetType puppetType = PuppetType.Male;

    [Header("材质颜色")]
    [SerializeField] private Color lightWoodColor = new Color(0.85f, 0.75f, 0.65f); // 浅枫木
    [SerializeField] private Color darkWoodColor = new Color(0.35f, 0.20f, 0.12f); // 深胡桃木
    [SerializeField] private Color hairColor = new Color(0.25f, 0.15f, 0.08f);      // 头发
    [SerializeField] private Color blushColor = new Color(0.95f, 0.70f, 0.60f);     // 腮红

    [Header("尺寸参数")]
    [SerializeField] private float height = 1.8f;
    [SerializeField] private float headScale = 0.35f;

    [Header("自动生成")]
    [SerializeField] private bool autoBuildOnStart = true;
    [SerializeField] private bool addPhysicsComponents = true;

    // 材质缓存
    private Material lightWoodMat;
    private Material darkWoodMat;
    private Material hairMat;
    private Material blushMat;

    private void Start()
    {
        if (autoBuildOnStart)
        {
            BuildPuppet();
        }
    }

    /// <summary>
    /// 构建完整木偶
    /// </summary>
    [ContextMenu("Build Puppet")]
    public void BuildPuppet()
    {
        // 清理旧部件
        ClearChildren();

        // 创建材质
        CreateMaterials();

        // 根据类型构建
        if (puppetType == PuppetType.Male)
            BuildMalePuppet();
        else
            BuildFemalePuppet();

        // 添加物理组件
        if (addPhysicsComponents)
            AddPhysicsComponents();

        Debug.Log($"[PuppetBuilder] {(puppetType == PuppetType.Male ? "男性" : "女性")}木偶生成完成");
    }

    #region 男性木偶

    private void BuildMalePuppet()
    {
        // 头部
        GameObject head = CreatePart("Head", Vector3.up * (height * 0.85f), Vector3.one * headScale, PrimitiveType.Sphere);
        head.GetComponent<Renderer>().material = lightWoodMat;

        // 鼻子
        GameObject nose = CreatePart("Nose", Vector3.up * (height * 0.85f) + Vector3.forward * (headScale * 0.4f), 
            new Vector3(0.08f, 0.12f, 0.08f), PrimitiveType.Cube);
        nose.GetComponent<Renderer>().material = lightWoodMat;
        nose.transform.rotation = Quaternion.Euler(0, 0, 45);

        // 眼睛
        CreateEye(Vector3.up * (height * 0.88f) + Vector3.forward * (headScale * 0.42f) + Vector3.right * 0.08f);
        CreateEye(Vector3.up * (height * 0.88f) + Vector3.forward * (headScale * 0.42f) - Vector3.right * 0.08f);

        // 眉毛
        CreateEyebrow(Vector3.up * (height * 0.92f) + Vector3.forward * (headScale * 0.40f) + Vector3.right * 0.08f);
        CreateEyebrow(Vector3.up * (height * 0.92f) + Vector3.forward * (headScale * 0.40f) - Vector3.right * 0.08f);

        // 嘴巴
        GameObject mouth = CreatePart("Mouth", Vector3.up * (height * 0.82f) + Vector3.forward * (headScale * 0.43f),
            new Vector3(0.12f, 0.02f, 0.02f), PrimitiveType.Cube);
        mouth.GetComponent<Renderer>().material = darkWoodMat;

        // 腮红
        CreateBlush(Vector3.up * (height * 0.84f) + Vector3.forward * (headScale * 0.38f) + Vector3.right * 0.12f);
        CreateBlush(Vector3.up * (height * 0.84f) + Vector3.forward * (headScale * 0.38f) - Vector3.right * 0.12f);

        // 头发
        BuildMaleHair();

        // 脖子
        GameObject neck = CreatePart("Neck", Vector3.up * (height * 0.72f), new Vector3(0.08f, 0.1f, 0.08f), PrimitiveType.Cylinder);
        neck.GetComponent<Renderer>().material = lightWoodMat;

        // 身体 - 西装外套
        GameObject suit = CreatePart("Suit", Vector3.up * (height * 0.55f), new Vector3(0.5f, 0.35f, 0.3f), PrimitiveType.Cube);
        suit.GetComponent<Renderer>().material = darkWoodMat;

        // 马甲
        GameObject vest = CreatePart("Vest", Vector3.up * (height * 0.55f) + Vector3.forward * 0.08f, 
            new Vector3(0.35f, 0.3f, 0.05f), PrimitiveType.Cube);
        vest.GetComponent<Renderer>().material = lightWoodMat;

        // 衬衫领口
        GameObject collar = CreatePart("Collar", Vector3.up * (height * 0.65f) + Vector3.forward * 0.05f,
            new Vector3(0.2f, 0.08f, 0.05f), PrimitiveType.Cube);
        collar.GetComponent<Renderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.95f, 0.95f, 0.90f) };

        // 纽扣
        for (int i = 0; i < 3; i++)
        {
            GameObject button = CreatePart($"Button_{i}", Vector3.up * (height * (0.5f - i * 0.06f)) + Vector3.forward * 0.16f,
                Vector3.one * 0.03f, PrimitiveType.Sphere);
            button.GetComponent<Renderer>().material = darkWoodMat;
        }

        // 手臂
        BuildArm(Vector3.up * (height * 0.6f) + Vector3.right * 0.32f, true);  // 左臂
        BuildArm(Vector3.up * (height * 0.6f) - Vector3.right * 0.32f, false); // 右臂

        // 腿
        BuildLeg(Vector3.up * (height * 0.25f) + Vector3.right * 0.12f, true);  // 左腿
        BuildLeg(Vector3.up * (height * 0.25f) - Vector3.right * 0.12f, false); // 右腿
    }

    private void BuildMaleHair()
    {
        // 头顶头发
        GameObject topHair = CreatePart("TopHair", Vector3.up * (height * 0.92f), 
            new Vector3(headScale * 1.1f, 0.1f, headScale * 1.1f), PrimitiveType.Cube);
        topHair.GetComponent<Renderer>().material = hairMat;

        // 侧分头发片
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 30f - 60f;
            Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * headScale * 0.5f, 
                height * 0.90f, Mathf.Cos(angle * Mathf.Deg2Rad) * headScale * 0.5f);
            GameObject hairPiece = CreatePart($"HairPiece_{i}", offset,
                new Vector3(0.15f, 0.08f, 0.05f), PrimitiveType.Cube);
            hairPiece.GetComponent<Renderer>().material = hairMat;
            hairPiece.transform.rotation = Quaternion.Euler(0, angle, 0);
        }
    }

    #endregion

    #region 女性木偶

    private void BuildFemalePuppet()
    {
        // 头部（比男性略小）
        GameObject head = CreatePart("Head", Vector3.up * (height * 0.85f), Vector3.one * (headScale * 0.9f), PrimitiveType.Sphere);
        head.GetComponent<Renderer>().material = lightWoodMat;

        // 鼻子
        GameObject nose = CreatePart("Nose", Vector3.up * (height * 0.84f) + Vector3.forward * (headScale * 0.38f),
            new Vector3(0.06f, 0.08f, 0.06f), PrimitiveType.Cube);
        nose.GetComponent<Renderer>().material = lightWoodMat;

        // 眼睛
        CreateEye(Vector3.up * (height * 0.87f) + Vector3.forward * (headScale * 0.38f) + Vector3.right * 0.07f);
        CreateEye(Vector3.up * (height * 0.87f) + Vector3.forward * (headScale * 0.38f) - Vector3.right * 0.07f);

        // 眉毛
        CreateEyebrow(Vector3.up * (height * 0.90f) + Vector3.forward * (headScale * 0.36f) + Vector3.right * 0.07f);
        CreateEyebrow(Vector3.up * (height * 0.90f) + Vector3.forward * (headScale * 0.36f) - Vector3.right * 0.07f);

        // 嘴巴
        GameObject mouth = CreatePart("Mouth", Vector3.up * (height * 0.81f) + Vector3.forward * (headScale * 0.39f),
            new Vector3(0.1f, 0.015f, 0.02f), PrimitiveType.Cube);
        mouth.GetComponent<Renderer>().material = darkWoodMat;

        // 腮红
        CreateBlush(Vector3.up * (height * 0.83f) + Vector3.forward * (headScale * 0.34f) + Vector3.right * 0.1f);
        CreateBlush(Vector3.up * (height * 0.83f) + Vector3.forward * (headScale * 0.34f) - Vector3.right * 0.1f);

        // 长发
        BuildFemaleHair();

        // 脖子
        GameObject neck = CreatePart("Neck", Vector3.up * (height * 0.74f), new Vector3(0.06f, 0.08f, 0.06f), PrimitiveType.Cylinder);
        neck.GetComponent<Renderer>().material = lightWoodMat;

        // 身体 - 吊带裙
        GameObject dress = CreatePart("Dress", Vector3.up * (height * 0.45f), new Vector3(0.35f, 0.45f, 0.25f), PrimitiveType.Cube);
        dress.GetComponent<Renderer>().material = lightWoodMat;

        // 方领
        GameObject neckline = CreatePart("Neckline", Vector3.up * (height * 0.62f) + Vector3.forward * 0.06f,
            new Vector3(0.18f, 0.06f, 0.03f), PrimitiveType.Cube);
        neckline.GetComponent<Renderer>().material = lightWoodMat;

        // 五颗纽扣
        for (int i = 0; i < 5; i++)
        {
            GameObject button = CreatePart($"Button_{i}", Vector3.up * (height * (0.55f - i * 0.05f)) + Vector3.forward * 0.14f,
                Vector3.one * 0.025f, PrimitiveType.Sphere);
            button.GetComponent<Renderer>().material = darkWoodMat;
        }

        // 腰带
        GameObject belt = CreatePart("Belt", Vector3.up * (height * 0.55f), new Vector3(0.37f, 0.04f, 0.27f), PrimitiveType.Cube);
        belt.GetComponent<Renderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit")) 
        { color = new Color(0.6f, 0.4f, 0.25f) };

        // 裙摆分瓣
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f;
            Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 0.15f, height * 0.25f, Mathf.Cos(angle * Mathf.Deg2Rad) * 0.12f);
            GameObject panel = CreatePart($"SkirtPanel_{i}", offset, new Vector3(0.08f, 0.2f, 0.03f), PrimitiveType.Cube);
            panel.GetComponent<Renderer>().material = lightWoodMat;
            panel.transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        // 手臂（比男性纤细）
        BuildFemaleArm(Vector3.up * (height * 0.58f) + Vector3.right * 0.25f, true);
        BuildFemaleArm(Vector3.up * (height * 0.58f) - Vector3.right * 0.25f, false);

        // 腿
        BuildFemaleLeg(Vector3.up * (height * 0.15f) + Vector3.right * 0.08f, true);
        BuildFemaleLeg(Vector3.up * (height * 0.15f) - Vector3.right * 0.08f, false);
    }

    private void BuildFemaleHair()
    {
        // 头顶
        GameObject topHair = CreatePart("TopHair", Vector3.up * (height * 0.90f),
            new Vector3(headScale * 0.95f, 0.08f, headScale * 0.95f), PrimitiveType.Cube);
        topHair.GetComponent<Renderer>().material = hairMat;

        // 长发片
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            float hairLength = 0.3f + Random.Range(-0.05f, 0.05f);
            Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * headScale * 0.45f,
                height * 0.82f - hairLength * 0.5f, Mathf.Cos(angle * Mathf.Deg2Rad) * headScale * 0.45f);
            GameObject hairPiece = CreatePart($"HairPiece_{i}", offset,
                new Vector3(0.12f, hairLength, 0.04f), PrimitiveType.Cube);
            hairPiece.GetComponent<Renderer>().material = hairMat;
            hairPiece.transform.rotation = Quaternion.Euler(0, angle, 0);
        }
    }

    #endregion

    #region 通用部件

    private void BuildArm(Vector3 shoulderPos, bool isLeft)
    {
        float side = isLeft ? 1 : -1;

        // 肩关节
        GameObject joint = CreatePart($"Shoulder_{(isLeft ? "L" : "R")}", shoulderPos, Vector3.one * 0.08f, PrimitiveType.Sphere);
        joint.GetComponent<Renderer>().material = darkWoodMat;

        // 上臂
        GameObject upperArm = CreatePart($"UpperArm_{(isLeft ? "L" : "R")}", 
            shoulderPos + Vector3.down * 0.15f + Vector3.right * side * 0.05f,
            new Vector3(0.1f, 0.25f, 0.1f), PrimitiveType.Cylinder);
        upperArm.GetComponent<Renderer>().material = darkWoodMat;

        // 肘关节
        GameObject elbow = CreatePart($"Elbow_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.3f + Vector3.right * side * 0.08f, Vector3.one * 0.07f, PrimitiveType.Sphere);
        elbow.GetComponent<Renderer>().material = darkWoodMat;

        // 前臂
        GameObject foreArm = CreatePart($"ForeArm_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.42f + Vector3.right * side * 0.1f,
            new Vector3(0.08f, 0.2f, 0.08f), PrimitiveType.Cylinder);
        foreArm.GetComponent<Renderer>().material = darkWoodMat;

        // 手腕
        GameObject wrist = CreatePart($"Wrist_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.55f + Vector3.right * side * 0.1f, Vector3.one * 0.06f, PrimitiveType.Sphere);
        wrist.GetComponent<Renderer>().material = lightWoodMat;

        // 手掌
        GameObject palm = CreatePart($"Palm_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.62f + Vector3.right * side * 0.1f,
            new Vector3(0.08f, 0.1f, 0.06f), PrimitiveType.Cube);
        palm.GetComponent<Renderer>().material = lightWoodMat;

        // 手指
        for (int i = 0; i < 4; i++)
        {
            GameObject finger = CreatePart($"Finger_{i}_{(isLeft ? "L" : "R")}",
                shoulderPos + Vector3.down * (0.66f + i * 0.02f) + Vector3.right * side * (0.07f + i * 0.015f),
                new Vector3(0.02f, 0.06f, 0.02f), PrimitiveType.Cylinder);
            finger.GetComponent<Renderer>().material = lightWoodMat;
        }
    }

    private void BuildFemaleArm(Vector3 shoulderPos, bool isLeft)
    {
        float side = isLeft ? 1 : -1;

        // 肩关节
        GameObject joint = CreatePart($"Shoulder_{(isLeft ? "L" : "R")}", shoulderPos, Vector3.one * 0.06f, PrimitiveType.Sphere);
        joint.GetComponent<Renderer>().material = lightWoodMat;

        // 上臂（纤细）
        GameObject upperArm = CreatePart($"UpperArm_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.12f + Vector3.right * side * 0.04f,
            new Vector3(0.07f, 0.2f, 0.07f), PrimitiveType.Cylinder);
        upperArm.GetComponent<Renderer>().material = lightWoodMat;

        // 肘关节
        GameObject elbow = CreatePart($"Elbow_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.25f + Vector3.right * side * 0.06f, Vector3.one * 0.05f, PrimitiveType.Sphere);
        elbow.GetComponent<Renderer>().material = lightWoodMat;

        // 前臂
        GameObject foreArm = CreatePart($"ForeArm_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.35f + Vector3.right * side * 0.08f,
            new Vector3(0.06f, 0.18f, 0.06f), PrimitiveType.Cylinder);
        foreArm.GetComponent<Renderer>().material = lightWoodMat;

        // 手腕
        GameObject wrist = CreatePart($"Wrist_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.46f + Vector3.right * side * 0.08f, Vector3.one * 0.05f, PrimitiveType.Sphere);
        wrist.GetComponent<Renderer>().material = lightWoodMat;

        // 手掌
        GameObject palm = CreatePart($"Palm_{(isLeft ? "L" : "R")}",
            shoulderPos + Vector3.down * 0.52f + Vector3.right * side * 0.08f,
            new Vector3(0.06f, 0.08f, 0.05f), PrimitiveType.Cube);
        palm.GetComponent<Renderer>().material = lightWoodMat;
    }

    private void BuildLeg(Vector3 hipPos, bool isLeft)
    {
        float side = isLeft ? 1 : -1;

        // 髋关节
        GameObject hip = CreatePart($"Hip_{(isLeft ? "L" : "R")}", hipPos, Vector3.one * 0.09f, PrimitiveType.Sphere);
        hip.GetComponent<Renderer>().material = darkWoodMat;

        // 大腿
        GameObject thigh = CreatePart($"Thigh_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.18f,
            new Vector3(0.12f, 0.3f, 0.12f), PrimitiveType.Cylinder);
        thigh.GetComponent<Renderer>().material = darkWoodMat;

        // 膝关节
        GameObject knee = CreatePart($"Knee_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.38f, Vector3.one * 0.08f, PrimitiveType.Sphere);
        knee.GetComponent<Renderer>().material = darkWoodMat;

        // 小腿
        GameObject shin = CreatePart($"Shin_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.52f,
            new Vector3(0.1f, 0.28f, 0.1f), PrimitiveType.Cylinder);
        shin.GetComponent<Renderer>().material = darkWoodMat;

        // 脚踝
        GameObject ankle = CreatePart($"Ankle_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.7f, Vector3.one * 0.07f, PrimitiveType.Sphere);
        ankle.GetComponent<Renderer>().material = darkWoodMat;

        // 脚
        GameObject foot = CreatePart($"Foot_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.76f + Vector3.forward * 0.05f,
            new Vector3(0.1f, 0.05f, 0.15f), PrimitiveType.Cube);
        foot.GetComponent<Renderer>().material = darkWoodMat;
    }

    private void BuildFemaleLeg(Vector3 hipPos, bool isLeft)
    {
        float side = isLeft ? 1 : -1;

        // 髋关节
        GameObject hip = CreatePart($"Hip_{(isLeft ? "L" : "R")}", hipPos, Vector3.one * 0.07f, PrimitiveType.Sphere);
        hip.GetComponent<Renderer>().material = lightWoodMat;

        // 大腿（纤细）
        GameObject thigh = CreatePart($"Thigh_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.15f,
            new Vector3(0.09f, 0.25f, 0.09f), PrimitiveType.Cylinder);
        thigh.GetComponent<Renderer>().material = lightWoodMat;

        // 膝关节
        GameObject knee = CreatePart($"Knee_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.32f, Vector3.one * 0.06f, PrimitiveType.Sphere);
        knee.GetComponent<Renderer>().material = lightWoodMat;

        // 小腿
        GameObject shin = CreatePart($"Shin_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.44f,
            new Vector3(0.07f, 0.22f, 0.07f), PrimitiveType.Cylinder);
        shin.GetComponent<Renderer>().material = lightWoodMat;

        // 脚踝
        GameObject ankle = CreatePart($"Ankle_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.58f, Vector3.one * 0.05f, PrimitiveType.Sphere);
        ankle.GetComponent<Renderer>().material = lightWoodMat;

        // 脚
        GameObject foot = CreatePart($"Foot_{(isLeft ? "L" : "R")}",
            hipPos + Vector3.down * 0.63f + Vector3.forward * 0.04f,
            new Vector3(0.07f, 0.04f, 0.12f), PrimitiveType.Cube);
        foot.GetComponent<Renderer>().material = lightWoodMat;
    }

    private GameObject CreatePart(string name, Vector3 position, Vector3 scale, PrimitiveType type)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(transform);
        part.transform.localPosition = position;
        part.transform.localScale = scale;

        // 移除碰撞器（只保留渲染）
        Collider col = part.GetComponent<Collider>();
        if (col != null) Destroy(col);

        return part;
    }

    private void CreateEye(Vector3 position)
    {
        GameObject eye = CreatePart("Eye", position, Vector3.one * 0.04f, PrimitiveType.Sphere);
        eye.GetComponent<Renderer>().material = darkWoodMat;
    }

    private void CreateEyebrow(Vector3 position)
    {
        GameObject brow = CreatePart("Eyebrow", position, new Vector3(0.06f, 0.015f, 0.02f), PrimitiveType.Cube);
        brow.GetComponent<Renderer>().material = lightWoodMat;
    }

    private void CreateBlush(Vector3 position)
    {
        GameObject blush = CreatePart("Blush", position, new Vector3(0.06f, 0.04f, 0.02f), PrimitiveType.Sphere);
        blush.GetComponent<Renderer>().material = blushMat;
    }

    #endregion

    #region 材质和物理

    private void CreateMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        lightWoodMat = new Material(urpLit) { color = lightWoodColor };
        lightWoodMat.SetFloat("_Smoothness", 0.3f);
        lightWoodMat.SetFloat("_Metallic", 0.0f);

        darkWoodMat = new Material(urpLit) { color = darkWoodColor };
        darkWoodMat.SetFloat("_Smoothness", 0.4f);
        darkWoodMat.SetFloat("_Metallic", 0.0f);

        hairMat = new Material(urpLit) { color = hairColor };
        hairMat.SetFloat("_Smoothness", 0.5f);

        blushMat = new Material(urpLit) { color = blushColor };
        blushMat.SetFloat("_Smoothness", 0.2f);
    }

    private void AddPhysicsComponents()
    {
        // 给根物体添加 Rigidbody 和 CapsuleCollider
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        CapsuleCollider col = gameObject.GetComponent<CapsuleCollider>();
        if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
        col.height = height * 0.8f;
        col.radius = 0.25f;
        col.center = new Vector3(0, height * 0.4f, 0);
    }

    private void ClearChildren()
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform)
        {
            children.Add(child);
        }
        foreach (Transform child in children)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    #endregion
}
