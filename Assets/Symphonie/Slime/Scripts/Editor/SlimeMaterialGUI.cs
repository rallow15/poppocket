using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace Symphonie.StoreAssets.Editor {
    public class SlimeMaterialGUI : ShaderGUI 
    {
        static Regex groupRegex = new Regex(@"^\s*Group\s*\((?<name>[^\)\,]*)\s*(\,\s*(?<order>\d+))?\s*\)");
        static Regex showIfRegex = new Regex(@"^\s*ShowIf\s*\((?<name>[^\)\,]*)\s*(\,\s*(?<order>\d+))?\s*\)");


        class Group
        {
            public string Name;
            public int Order = int.MaxValue;
            public List<MaterialProperty> Properties = new List<MaterialProperty>();
        }

        class ShowIf
        {
            public string Property;
            public string DependOnProperty;
        }

        List<Group> CachedPropGroups = new List<Group>();
        int CachedPropertyHash = 0;

        Dictionary<string, ShowIf> CachedShowIfs = new Dictionary<string, ShowIf>();

        void UpdatePropertyCache(MaterialEditor materialEditor, MaterialProperty[] properties) {
            Dictionary<string, Group> grps = new Dictionary<string, Group>();
            CachedShowIfs.Clear();

            var shader = (materialEditor.target as Material).shader;
            foreach(var p in properties) {
                if(p.propertyFlags.HasFlag(ShaderPropertyFlags.HideInInspector))
                    continue;

                int idx = shader.FindPropertyIndex(p.name);
                var attrs = shader.GetPropertyAttributes(idx);
                bool anyGroup = false;
                foreach (var a in attrs)
                {
                    // match group
                    var m = groupRegex.Match(a);
                    if (m.Success)
                    {
                        string grpName = m.Groups["name"].Value;
                        string order = m.Groups["order"].Value;

                        if (!grps.TryGetValue(grpName, out var grp))
                        {
                            grp = new Group { Name = grpName };
                            grps.Add(grpName, grp);
                        }

                        if (grp.Order == int.MaxValue && !string.IsNullOrEmpty(order))
                        {
                            grp.Order = int.Parse(order);
                        }
                        grp.Properties.Add(p);
                        anyGroup = true;
                    }

                    // match ShowIf
                    m = showIfRegex.Match(a);
                    if (m.Success)
                    {                        
                        string dep = m.Groups["name"].Value;
                        CachedShowIfs.Add(p.name, new ShowIf{Property = p.name, DependOnProperty = dep});
                    }

                }
                if(!anyGroup) {
                    if(!grps.ContainsKey("Misc"))
                        grps.Add("Misc", new Group{ Name = "Misc" });
                    var grp = grps["Misc"];
                    grp.Properties.Add(p);
                }
            }
            CachedPropGroups = grps.Values.ToList();
            CachedPropGroups.Sort((x,y)=>x.Order.CompareTo(y.Order));

            CachedPropertyHash = shader.GetHashCode();
        }


        Dictionary<string, MaterialProperty> NameToProp = new Dictionary<string, MaterialProperty>();
        Dictionary<string, bool> ShowingProperties = new Dictionary<string, bool>();

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var shader = (materialEditor.target as Material).shader;

            NameToProp.Clear();
            ShowingProperties.Clear();
            foreach (var p in properties)
                NameToProp.Add(p.name, p);
            foreach (var p in properties)
            {
                ShowingProperties[p.name] = true;
                if (CachedShowIfs.TryGetValue(p.name, out var showIf) && NameToProp.TryGetValue(showIf.DependOnProperty, out var dep))
                {
                    if (dep.propertyType == ShaderPropertyType.Int && dep.intValue == 0)
                        ShowingProperties[p.name] = false;
                    else if (dep.propertyType == ShaderPropertyType.Float && dep.floatValue == 0)
                        ShowingProperties[p.name] = false;
                    //Debug.Log($"{p.name}: {ShowingProperties[p.name]}");
                }
            }



            //if (CachedPropGroups == null || CachedPropertyHash != shader.GetHashCode())
            UpdatePropertyCache(materialEditor, properties);

            foreach (var grp in CachedPropGroups)
            {
                bool skip = grp.Properties.All(p => !ShowingProperties[p.name]);
                if (skip)
                    continue;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label(grp.Name, EditorStyles.boldLabel);
                    }

                    foreach (var p in grp.Properties)
                    {
                        if (ShowingProperties[p.name])
                            materialEditor.ShaderProperty(p, p.displayName);
                    }
                }

            }

            materialEditor.serializedObject.ApplyModifiedProperties();


        }
    }

}
