using System.Runtime.InteropServices;
using UnityEngine;
using System;

public class ObjSpawner : MonoBehaviour
{
    struct ObjData
    {
        public Vector3 pos;
        public Vector3 movingPos;
        public Vector3 adjustPos;
        public Vector3 showPos;
        public float colourDelta;
        public float vertDelta;
        public int colourGrp;
        public float effectStartTime;
        public int reactEffectMode;
    }

    // Properties
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    [Header("Spawn Setting")]
    [SerializeField] int _Instances = 18000;                            // Total Amount
    [SerializeField] Vector3 _MaxPos = new Vector3(400,200,250);        // Spawn Range
    [SerializeField] Vector3 _MeshScale = new Vector3(3, 3, 3);         // Scale
    [SerializeField] ComputeShader _ComputeShader;                      // Compute Shader

    [Header("Animation")]
    [SerializeField] Mesh _ObjMesh;                                     // Shape / Style
    [SerializeField] Material _ObjMat;                                  // Material (Texture)
    [SerializeField] float _ObjMoveSpeed = 0.01f;                       // Moving Speed
    [SerializeField] float _ColourChangeSpeed = 8f;                     // Colour Change Speed

    // Cursor Position Detection
    private Vector3 screenPos;
    private Vector3 cursorPos;

    // Click Detection & Settings
    private bool clicking = false;
    private bool holding = false;
    private float lastClick = 0;
    private const float clickHoldThreshold = 0.5f;
    private float lastRelease = 0.0f;
    private const float clickBuffer = 0.3f;

    // Reactions
    [Header("Reaction")]
    [SerializeField] float reactTimeLimit = 7;
    private int reactCount = 0;
    private int[] reactModeList = new int[4 * 10];
    private float[] reactCursorListX = new float[4 * 10];
    private float[] reactCursorListY = new float[4 * 10];
    private float[] reactCursorListZ = new float[4 * 10];
    private float[] reactStartTimeList = new float[4 * 10];
    // Multiply by 4
    // Ref: https://docs.unity3d.com/ScriptReference/ComputeShader.SetFloats.html

    // GPU
    private ComputeBuffer _ObjDataBuffer;
    private ComputeBuffer _GpuInstancingArgsBuffer;
    uint[] _GPUInstancingArgs = new uint[5] { 0, 0, 0, 0, 0 };

    // Start is called before the first frame update
    void Start()
    {
        // Buffer Init
        this._ObjDataBuffer = new ComputeBuffer(this._Instances, Marshal.SizeOf(typeof(ObjData)) );
        this._GpuInstancingArgsBuffer = new ComputeBuffer(1, this._GPUInstancingArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        // Buffer Settings
        var posArr = new ObjData[this._Instances];

        int side = (int)Math.Floor(Math.Sqrt(this._Instances));
        float space = 2 * _MaxPos.x / side;
        for (int i=0; i<this._Instances; ++i)
        {
            posArr[i].pos = new Vector3
            (
                UnityEngine.Random.Range(_MaxPos.x, -_MaxPos.x),
                UnityEngine.Random.Range(_MaxPos.y, -_MaxPos.y),
                UnityEngine.Random.Range(_MaxPos.z, -_MaxPos.z)
            );
            posArr[i].movingPos = Vector3.zero;
            posArr[i].adjustPos = Vector3.zero;
            posArr[i].showPos = posArr[i].pos;
            posArr[i].colourDelta = 0.0f;
            posArr[i].vertDelta = UnityEngine.Random.Range(0,10f);
            posArr[i].colourGrp = UnityEngine.Random.Range(0,3);
            posArr[i].effectStartTime = 0.0f;
            posArr[i].reactEffectMode = 0;
        }
        this._ObjDataBuffer.SetData(posArr);

        // On Fin
        posArr = null;
    }

    // Update is called once per frame
    void Update()
    {
        // Set Cursor Stat
        screenPos = Input.mousePosition;
        screenPos.z = _MaxPos.z;
        cursorPos = mainCamera.ScreenToWorldPoint(screenPos);

        // Initilization
        while (UpdateReactList()) ;

        int reactMode = OnClickSetReact();
        ManageReactList(reactMode);

        float t = (Time.time / _ColourChangeSpeed) % 4;
        int phase = (int)Math.Floor(t);  // 0 -> (4

        // Section 1: Compute Shader
        int kernelID = this._ComputeShader.FindKernel("MainCS");

        // Set Compute Shader's Required Variables
        this._ComputeShader.SetFloat("_Time", Time.time);
        this._ComputeShader.SetFloat("_maxX", _MaxPos.x);
        this._ComputeShader.SetFloat("_maxZ", _MaxPos.z);
        this._ComputeShader.SetFloat("_ObjMoveSpeed", _ObjMoveSpeed);

        this._ComputeShader.SetFloat("_ReactMode", reactMode);
        this._ComputeShader.SetFloat("_ReactTimeLimit", reactTimeLimit);
        this._ComputeShader.SetInt("_ReactCount", reactCount);
        this._ComputeShader.SetInts("_ReactMode", reactModeList);
        this._ComputeShader.SetFloats("_CursorPosX", reactCursorListX);
        this._ComputeShader.SetFloats("_CursorPosY", reactCursorListY);
        this._ComputeShader.SetFloats("_CursorPosZ", reactCursorListZ);
        this._ComputeShader.SetFloats("_ReactStartTime", reactStartTimeList);


        // Set Buffer
        this._ComputeShader.SetBuffer(kernelID, "_ObjDataBuffer", this._ObjDataBuffer);

        // Dispatch
        this._ComputeShader.Dispatch(kernelID, (Mathf.CeilToInt(this._Instances / 256) + 1), 1, 1);

        // Set colour change

        // Section 2: GPU Instancing
        this._GPUInstancingArgs[0] = (this._ObjMesh != null) ? this._ObjMesh.GetIndexCount(0) : 0;
        this._GPUInstancingArgs[1] = (uint)this._Instances;
        this._GpuInstancingArgsBuffer.SetData(this._GPUInstancingArgs);
        this._ObjMat.SetBuffer("_ObjDataBuffer", this._ObjDataBuffer);
        this._ObjMat.SetInt("_Phase", phase);
        this._ObjMat.SetFloat("_colourChangeFactor", (t-phase));
        this._ObjMat.SetVector("_MeshScale", this._MeshScale);
        Graphics.DrawMeshInstancedIndirect(this._ObjMesh, 0, this._ObjMat, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), this._GpuInstancingArgsBuffer);
    }

    void OnDestroy()
    {
        if (this._ObjDataBuffer != null)
        {
            this._ObjDataBuffer.Release();
            this._ObjDataBuffer = null;
        }
        if (this._GpuInstancingArgsBuffer != null)
        {
            this._GpuInstancingArgsBuffer.Release();
            this._GpuInstancingArgsBuffer = null;
        }
    }

    private int OnClickSetReact()
    {
        //if (lastRelease != 0)
        //    if (Time.time - lastRelease <= clickBuffer) return 0;
        //    else lastRelease = 0.0f;

        // React Mode
        // 1 - Click
        // 2 - Hold
        // 3 - Release
        int reactMode = 0;

        if (Input.GetMouseButton(0))
        {
            if (!clicking)
            {
                clicking = true;
                lastClick = Time.time;
            }
            //else
            //{
            //    if ((Time.time - lastClick) >= clickHoldThreshold)
            //    {
            //        if (!holding)
            //        {
            //            holding = true;
            //            reactMode = 2;
            //        }
            //    }
            //}
        }
        else
        {
            if (clicking)
            {
                if ((Time.time - lastClick) < clickHoldThreshold)
                {
                    reactMode = 1;
                }
                //else
                //{
                //    Debug.Log("3");
                //    RemoveFirstFromReactList(); // Hold must be first in list
                //    Debug.Log("Execute: Remove React 2 from list");
                //    reactMode = 3;
                //}

                //lastRelease = Time.time;
            }
            clicking = false;
            //holding = false;
        }

        return reactMode;
    }

    private void ManageReactList(int reactMode)
    {
        if (reactMode != 0 && reactCount < 10)
        {
            reactModeList[4 * reactCount] = reactMode;
            reactCursorListX[4 * reactCount] = cursorPos.x;
            reactCursorListY[4 * reactCount] = cursorPos.y;
            reactCursorListZ[4 * reactCount] = cursorPos.z;
            reactStartTimeList[4 * reactCount] = Time.time;
            reactCount += 1;
        }
    }

    private bool UpdateReactList()
    {
        if (reactCount == 0) return false;

        if (reactModeList[0] == 1)
        {
            if ((Time.time - reactStartTimeList[0]) >= reactTimeLimit)
            {
                RemoveFirstFromReactList();
                return true;
            }
        }
        else if (reactModeList[0] == 2)
        {
            if (!clicking && !holding)
            {
                RemoveFirstFromReactList();
                return true;
            }
        }
        for (int i = 0; i < reactCount - 1; i++)
        {
            if (reactModeList[0] == 3)
            {
                if ((Time.time - reactStartTimeList[0]) >= 2)
                {
                    ManageReactListRemove(i);
                    return true;
                }
            }
        }
        return false;
    }

    private void RemoveFirstFromReactList()
    {
        for (int i=0; i< reactCount-1; i++)
        {
            reactModeList       [4 * i]   = reactModeList       [4 * (i + 1)];
            reactCursorListX    [4 * i]   = reactCursorListX    [4 * (i + 1)];
            reactCursorListY    [4 * i]   = reactCursorListY    [4 * (i + 1)];
            reactCursorListZ    [4 * i]   = reactCursorListZ    [4 * (i + 1)];
            reactStartTimeList  [4 * i]   = reactStartTimeList  [4 * (i + 1)];
        }

        reactModeList         [4 * (reactCount - 1)] = 0;
        reactCursorListX      [4 * (reactCount - 1)] = 0;
        reactCursorListY      [4 * (reactCount - 1)] = 0;
        reactCursorListZ      [4 * (reactCount - 1)] = 0;
        reactStartTimeList    [4 * (reactCount - 1)] = 0;

        reactCount -= 1;
    }

    private void ManageReactListRemove(int index)
    {
        for (int j = index; j < reactCount - 1; j++)
        {
            reactModeList       [4 * j] = reactModeList        [4 * (j + 1)];
            reactCursorListX    [4 * j] = reactCursorListX     [4 * (j + 1)];
            reactCursorListY    [4 * j] = reactCursorListY     [4 * (j + 1)];
            reactCursorListZ    [4 * j] = reactCursorListZ     [4 * (j + 1)];
            reactStartTimeList  [4 * j] = reactStartTimeList   [4 * (j + 1)];
        }

        reactModeList       [4 * (reactCount - 1)] = 0;
        reactCursorListX    [4 * (reactCount - 1)] = 0;
        reactCursorListY    [4 * (reactCount - 1)] = 0;
        reactCursorListZ    [4 * (reactCount - 1)] = 0;
        reactStartTimeList  [4 * (reactCount - 1)] = 0;

        reactCount -= 1;
                

    }

}