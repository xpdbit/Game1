using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.GamePlay
{
    /// <summary>
    /// Runtime visual generator for simulation (no prefabs)
    /// </summary>
    public class ASimulatedVisuals
    {
        private GameObject _root;
        private List<GameObject> _createdObjects = new List<GameObject>();
        private Material _defaultMaterial;
        private Material _transparentMaterial;

        public ASimulatedVisuals()
        {
            _root = new GameObject("SimVisuals");
            _root.transform.position = Vector3.zero;

            // Create default material
            _defaultMaterial = CreateStandardMaterial(Color.gray);

            // Create transparent material (using TransparentBlack shader concept)
            _transparentMaterial = CreateTransparentMaterial(Color.white);
        }

        #region Material Creation
        /// <summary>
        /// Create a standard material
        /// </summary>
        public Material CreateStandardMaterial(Color color, float metallic = 0f, float smoothness = 0.5f)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }

        /// <summary>
        /// Create a transparent material
        /// </summary>
        public Material CreateTransparentMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.5f));
            mat.SetFloat("_Surface", 1); // Transparent
            return mat;
        }

        /// <summary>
        /// Create transparent black material (mirrors TransparentBlack shader behavior)
        /// </summary>
        public Material CreateTransparentBlackMaterial()
        {
            var shader = Shader.Find("Custom/TransparentBlack");
            if (shader == null)
            {
                Debug.LogWarning("[ASimulatedVisuals] Custom/TransparentBlack shader not found, using fallback");
                return CreateTransparentMaterial(Color.black);
            }

            var mat = new Material(shader);
            mat.SetColor("_Color", Color.black);
            mat.SetFloat("_Threshold", 0.1f);
            return mat;
        }

        /// <summary>
        /// Create URP Lit material
        /// </summary>
        public Material CreateURPMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            return mat;
        }
        #endregion

        #region Primitive Creation
        /// <summary>
        /// Create a cube GameObject
        /// </summary>
        public GameObject CreateCube(Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"SimCube_{_createdObjects.Count}";
            go.transform.SetParent(_root.transform);
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreateStandardMaterial(color);

            _createdObjects.Add(go);
            return go;
        }

        /// <summary>
        /// Create a sphere GameObject
        /// </summary>
        public GameObject CreateSphere(Vector3 position, float radius, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"SimSphere_{_createdObjects.Count}";
            go.transform.SetParent(_root.transform);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * radius * 2;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreateStandardMaterial(color);

            _createdObjects.Add(go);
            return go;
        }

        /// <summary>
        /// Create a capsule GameObject
        /// </summary>
        public GameObject CreateCapsule(Vector3 position, float height, float radius, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"SimCapsule_{_createdObjects.Count}";
            go.transform.SetParent(_root.transform);
            go.transform.position = position;
            go.transform.localScale = new Vector3(radius * 2, height / 2, radius * 2);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreateStandardMaterial(color);

            _createdObjects.Add(go);
            return go;
        }

        /// <summary>
        /// Create a plane GameObject
        /// </summary>
        public GameObject CreatePlane(Vector3 position, Vector2 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = $"SimPlane_{_createdObjects.Count}";
            go.transform.SetParent(_root.transform);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x / 10, 1, size.y / 10);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreateStandardMaterial(color);

            _createdObjects.Add(go);
            return go;
        }
        #endregion

        #region Mesh Generation
        /// <summary>
        /// Create a quad mesh GameObject
        /// </summary>
        public GameObject CreateQuad(Vector3 position, Quaternion rotation, Vector2 size, Material mat)
        {
            var go = new GameObject($"SimQuad_{_createdObjects.Count}");
            go.transform.SetParent(_root.transform);
            go.transform.position = position;
            go.transform.rotation = rotation;

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateQuadMesh(size);

            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.material = mat ?? _defaultMaterial;

            _createdObjects.Add(go);
            return go;
        }

        /// <summary>
        /// Create quad mesh
        /// </summary>
        public static Mesh CreateQuadMesh(Vector2 size)
        {
            var mesh = new Mesh();
            mesh.name = "RuntimeQuad";

            float w = size.x / 2;
            float h = size.y / 2;

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-w, -h, 0),
                new Vector3(w, -h, 0),
                new Vector3(-w, h, 0),
                new Vector3(w, h, 0)
            };

            int[] triangles = new int[6] { 0, 2, 1, 1, 2, 3 };
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Create a grid of cubes (for terrain or displays)
        /// </summary>
        public GameObject CreateCubeGrid(Vector3 startPosition, int width, int height, float spacing, Color baseColor)
        {
            var grid = new GameObject($"SimCubeGrid_{_createdObjects.Count}");
            grid.transform.SetParent(_root.transform);
            grid.transform.position = startPosition;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float hue = (float)(x + y) / (width + height);
                    Color color = Color.HSVToRGB(hue, 0.8f, 0.9f);

                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"GridCell_{x}_{y}";
                    cube.transform.SetParent(grid.transform);
                    cube.transform.position = new Vector3(x * spacing, y * spacing, 0);
                    cube.transform.localScale = Vector3.one * (spacing * 0.9f);

                    var renderer = cube.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material = CreateStandardMaterial(color);
                }
            }

            _createdObjects.Add(grid);
            return grid;
        }

        /// <summary>
        /// Create a simple character representation (capsule body + sphere head)
        /// </summary>
        public GameObject CreateSimpleCharacter(Vector3 position, Color bodyColor)
        {
            var character = new GameObject($"SimCharacter_{_createdObjects.Count}");
            character.transform.SetParent(_root.transform);
            character.transform.position = position;

            // Body (capsule)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(character.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = Vector3.one * 0.5f;

            var bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
                bodyRenderer.material = CreateStandardMaterial(bodyColor);

            // Head (sphere)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(character.transform);
            head.transform.localPosition = new Vector3(0, 0.7f, 0);
            head.transform.localScale = Vector3.one * 0.3f;

            var headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null)
                headRenderer.material = CreateStandardMaterial(bodyColor);

            _createdObjects.Add(character);
            return character;
        }
        #endregion

        #region Scene Setup
        /// <summary>
        /// Create camera for simulation
        /// </summary>
        public Camera CreateCamera(Vector3 position, Vector3 lookAt)
        {
            var camObj = new GameObject("SimCamera");
            camObj.transform.SetParent(_root.transform);

            var cam = camObj.AddComponent<Camera>();
            cam.transform.position = position;
            cam.transform.LookAt(lookAt);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = AConfig.Active.backgroundColor;
            cam.fieldOfView = 60f;

            _createdObjects.Add(camObj);
            return cam;
        }

        /// <summary>
        /// Create directional light
        /// </summary>
        public Light CreateDirectionalLight(Vector3 direction, float intensity)
        {
            var lightObj = new GameObject("SimLight");
            lightObj.transform.SetParent(_root.transform);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.LookRotation(direction);
            light.intensity = intensity;
            light.color = Color.white;

            _createdObjects.Add(lightObj);
            return light;
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Destroy all created objects
        /// </summary>
        public void DestroyAll()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                    UnityEngine.Object.Destroy(obj);
            }
            _createdObjects.Clear();

            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        /// <summary>
        /// Get root object
        /// </summary>
        public GameObject Root => _root;

        /// <summary>
        /// Get all created objects
        /// </summary>
        public IReadOnlyList<GameObject> CreatedObjects => _createdObjects;
        #endregion
    }
}