using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Symphonie.StoreAssets.Editor
{
    /// <summary>
    /// A simple wizard to generate LUT for slime core rendering
    /// </summary>
    public class CoreScatterLUTBakerWizard : ScriptableWizard
    {
        [MenuItem("Tools/Symphonie/Utilities/Core Scatter LUT Baker")]
        static void Init()
        {
            CoreScatterLUTBakerWizard window = (CoreScatterLUTBakerWizard)EditorWindow.GetWindow(typeof(CoreScatterLUTBakerWizard));
            window.titleContent = new GUIContent("Core Scatter LUT Baker");
        }

        public enum KernelType {
            Gaussian,
            Gaussian5,
            Linear,
            Quadratic,
            InvertSquare,
            SmoothStep,
            CustomCurve,
        }

        public enum CoreShapeType {
            Discrete,
            CustomCurve
        }

        public enum SaveFormat {
            EXR,
            PNG,
            
        }

        

        public KernelType Kernel = KernelType.Quadratic;
        public AnimationCurve KernelCustomCurve = AnimationCurve.Linear(0, 1, 1, 0);

        public CoreShapeType CoreShape = CoreShapeType.Discrete;
        public AnimationCurve CoreShapeCustonCurve = AnimationCurve.Linear(0, 1, 1, 0);

        public Vector3 ScatterWidthScale = new Vector3(5, 2, 1);

        public int SampleCount = 1024;
        public int ScatterWidthResolution = 64;
        public int OffsetResolution = 64;

        public float ScatterWidthEncodeFactor = 4;
        public float OffsetEncodeFactor = 4;

        public SaveFormat Format = SaveFormat.EXR;
        public string OutputPath = "Assets/Symphonie/Models/Slime/Shaders/SlimeCoreScatterLUT.exr";

        static readonly float GaussianWidthScale = 2.2f;


        void GenerateKernel_Gaussian(float[] weights) {
            float sum = 0;
            for (int i = 0; i < weights.Length; ++i) {
                float x = (float)i / (weights.Length - 1);
                float xs = x * GaussianWidthScale;
                weights[i] = Mathf.Exp(-xs * xs);
                sum += weights[i];
            }

            // normalize weights
            sum *=  2;
            for (int i = 0; i < weights.Length; ++i) 
                weights[i] /= sum;        
        }

        void GenerateKernel_Gaussian5(float[] weights) {
            float sum = 0;
            for (int i = 0; i < weights.Length; ++i) {
                float x = (float)i / (weights.Length - 1);
                float xs1 = x * 2;
                float xs2 = xs1 * 2;
                float xs3 = xs2 * 2;
                float xs4 = xs3 * 2;
                float xs5 = xs4 * 2;
                weights[i] = 
                    Mathf.Exp(-xs1 * xs1) + 
                    Mathf.Exp(-xs2 * xs2) + 
                    Mathf.Exp(-xs3 * xs3) +
                    Mathf.Exp(-xs4 * xs4) + 
                    Mathf.Exp(-xs5 * xs5);
                sum += weights[i];
            }

            // normalize weights
            sum *=  2;
            for (int i = 0; i < weights.Length; ++i) 
                weights[i] /= sum;        
        }

        void GenerateKernel_Quadratic(float[] weights) {
            float sum = 0;
            for (int i = 0; i < weights.Length; ++i) {
                float x = (float)i / (weights.Length - 1);
                weights[i] = Mathf.Pow(1-x, 4);
                sum += weights[i];
            }

            // normalize weights
            sum *= 2;
            for (int i = 0; i < weights.Length; ++i) 
                weights[i] /= sum;
            
        }

        void GenerateKernel_Linear(float[] weights) {
            float sum = 0;
            for (int i = 0; i < weights.Length; ++i) {
                float x = (float)i / (weights.Length - 1);
                weights[i] = 1 - x;
                sum += weights[i];
            }

            // normalize weights
            sum *= 2;
            for (int i = 0; i < weights.Length; ++i) 
                weights[i] /= sum;
            
        }

        void GenerateKernel_InvertSquare(float[] weights) {
            float scale = 8;
            float bias = 1 / (scale + 1);
            float sum = 0;
            for (int i = 0; i < weights.Length; ++i) {
                float x = (float)i / (weights.Length - 1);
                weights[i] = 1.0f / (x * x * scale + 1) / (1-bias)  - bias;
                sum += weights[i];
            }

            // normalize weights
            sum *= 2;
            for (int i = 0; i < weights.Length; ++i) 
                weights[i] /= sum;
            
        }

        void GenerateKernel_CustomCurve(float[] weights) {
            float sum = 0;
            for (int i = 0; i < weights.Length; ++i) {
                float x = (float)i / (weights.Length - 1);
                weights[i] = KernelCustomCurve.Evaluate(x);
                sum += weights[i];
            }

            // normalize weights
            sum *= 2;
            for (int i = 0; i < weights.Length; ++i) 
                weights[i] /= sum;
            
        }


        float CoreFunc(AnimationCurve shapeCurve, float radius, float x) {
            if (CoreShape == CoreShapeType.CustomCurve) {
                return shapeCurve.Evaluate(Mathf.Abs(x));
            }        
            return Mathf.Abs(x) > radius ? 0 : 1;
        }

        float Convolute(AnimationCurve shapeCurve, float[] gaussian, float width, float offset) {
            float sum = 0;
            float radius = 1;
            
            for (int i = 0; i < gaussian.Length; ++i) {
                float w = gaussian[i];
                float x = (float)i / (gaussian.Length - 1);
                float xs = x * width;

                float pos = CoreFunc(shapeCurve, radius, offset + xs);
                float neg = CoreFunc(shapeCurve, radius, offset - xs);

                sum += w * pos + w * neg;
            }
            return sum;
        }
        

        void OnWizardCreate() {
            Debug.Log("Generating LUT");

            float[] gaussian = new float[SampleCount];

            switch(Kernel) {
                case KernelType.Gaussian:
                    GenerateKernel_Gaussian(gaussian); break;
                case KernelType.Gaussian5:
                    GenerateKernel_Gaussian5(gaussian); break;
                case KernelType.Quadratic:
                    GenerateKernel_Quadratic(gaussian); break;
                case KernelType.Linear:
                    GenerateKernel_Linear(gaussian); break;
                case KernelType.InvertSquare:
                    GenerateKernel_InvertSquare(gaussian); break;
                default:
                    GenerateKernel_CustomCurve(gaussian); break;
            }
            

            Task<Vector4[]>[] tasks = new Task<Vector4[]>[ScatterWidthResolution];

            Texture2D lut = new Texture2D(OffsetResolution, ScatterWidthResolution, TextureFormat.RGBAFloat, false);
                
            try {
                Vector4[] pixels = new Vector4[OffsetResolution * ScatterWidthResolution];

                for (int y = 0; y < ScatterWidthResolution; ++y) {
                    float ry = (float)(y + 1) / ScatterWidthResolution;
                    float width = ScatterWidthEncodeFactor / ry - ScatterWidthEncodeFactor;

                    var CoreCurve = new AnimationCurve(CoreShapeCustonCurve.keys);

                    tasks[y] = Task.Run(() => {
                        Vector4[] row = new Vector4[OffsetResolution];
                        for (int x = 0; x < OffsetResolution; ++x) {
                            float rx = (float)(x + 0) / OffsetResolution;
                            float offset = OffsetEncodeFactor / (1 - rx) - OffsetEncodeFactor;
                            row[x] = new Vector4(
                                Convolute(CoreCurve, gaussian, width * ScatterWidthScale.x, offset),
                                Convolute(CoreCurve, gaussian, width * ScatterWidthScale.y, offset),
                                Convolute(CoreCurve, gaussian, width * ScatterWidthScale.z, offset),
                                1 );
                        }
                        return row;
                    });
                }

                Task.WaitAll(tasks);
                for (int y = 0; y < ScatterWidthResolution; ++y) {
                    tasks[y].Result.CopyTo(pixels, y * OffsetResolution);
                }

                lut.SetPixelData(pixels, 0);
                lut.Apply();

                if(Format == SaveFormat.EXR) {
                    string path = System.IO.Path.ChangeExtension(OutputPath, ".exr");
                    var bytes = lut.EncodeToEXR();
                    System.IO.File.WriteAllBytes(path, bytes);
                    AssetDatabase.ImportAsset(path);
                }
                else if (Format == SaveFormat.PNG) {
                    string path = System.IO.Path.ChangeExtension(OutputPath, ".png");
                    var bytes = lut.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, bytes);
                    AssetDatabase.ImportAsset(path);
                }

                
            }
            catch {
                Debug.LogError("Failed to generate LUT");
            }
            finally {
                DestroyImmediate(lut);
            }

        }


        
    }

}