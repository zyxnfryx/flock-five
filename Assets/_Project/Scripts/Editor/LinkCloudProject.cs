#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.Networking;

namespace FlockFive.Editor
{
    // Binds this Editor project to a Unity Cloud / dashboard project named
    // "Flock Five" so LevelPlay can see it. Runs once after the Editor is signed in.
    [InitializeOnLoad]
    static class LinkCloudProject
    {
        const string ProjectName = "Flock Five";
        const string PrefDone = "flockfive.cloud.link.v1";
        const string Core = "https://core.cloud.unity3d.com/api";

        static LinkCloudProject()
        {
            Debug.Log("Flock Five: cloud linker loaded. bound=" + CloudProjectSettings.projectBound +
                " id=" + CloudProjectSettings.projectId + " user=" + CloudProjectSettings.userName);
            EditorApplication.delayCall += TryLink;
        }

        [MenuItem("Flock Five/Link Unity Cloud Project")]
        public static void MenuLink()
        {
            EditorPrefs.DeleteKey(PrefDone);
            TryLink();
        }

        [MenuItem("Flock Five/Open Unity Dashboard")]
        public static void OpenDashboard()
        {
            string org = CloudProjectSettings.organizationId;
            string id = CloudProjectSettings.projectId;
            if (string.IsNullOrEmpty(id))
            {
                Application.OpenURL("https://cloud.unity.com/home");
                return;
            }
            string url = string.IsNullOrEmpty(org)
                ? "https://cloud.unity.com/home/projects/" + id
                : "https://cloud.unity.com/home/organizations/" + org + "/projects/" + id;
            Application.OpenURL(url);
        }

        static void TryLink()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!string.IsNullOrEmpty(CloudProjectSettings.projectId) &&
                CloudProjectSettings.projectBound)
            {
                EnableConnect();
                return;
            }
            if (EditorPrefs.GetBool(PrefDone, false)) return;

            string token = CloudProjectSettings.accessToken;
            string user = CloudProjectSettings.userName;
            if (string.IsNullOrEmpty(token) ||
                string.IsNullOrEmpty(user) ||
                user.Equals("anonymous", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Flock Five: Unity account not signed in — opening login so we can create the cloud project.");
                ShowLogin();
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.delayCall += TryLink;
                };
                return;
            }

            EditorApplication.delayCall += () => Run(token);
        }

        static async void Run(string token)
        {
            try
            {
                var orgsJson = await Get(Core + "/orgs", token);
                var orgs = ParseOrgs(orgsJson);
                if (orgs.Count == 0)
                {
                    Debug.LogWarning("Flock Five: no Unity organizations on this account. Create one at cloud.unity.com then run Flock Five > Link Unity Cloud Project.");
                    Application.OpenURL("https://cloud.unity.com/home");
                    return;
                }

                Org org = orgs[0];
                for (int i = 0; i < orgs.Count; i++)
                {
                    if (orgs[i].Name.IndexOf("zfx", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        orgs[i].Name.IndexOf("flock", StringComparison.OrdinalIgnoreCase) >= 0)
                        org = orgs[i];
                }

                string listJson = await Get(Core + "/orgs/" + org.Id + "/projects", token);
                string existing = FindProjectId(listJson, ProjectName);
                string guid = existing;
                string name = ProjectName;
                if (string.IsNullOrEmpty(guid))
                {
                    string body = "{\"name\":\"" + ProjectName + "\",\"coppa\":\"not_compliant\"}";
                    string created = await Post(Core + "/orgs/" + org.Id + "/projects", token, body);
                    guid = FindGuid(created);
                    name = FindString(created, "name");
                    if (string.IsNullOrEmpty(name)) name = ProjectName;
                    Debug.Log("Flock Five: created Unity Cloud project " + name + " (" + guid + ") in " + org.Name);
                }
                else
                    Debug.Log("Flock Five: found existing Unity Cloud project " + ProjectName + " (" + guid + ")");

                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogError("Flock Five: cloud project create did not return a guid. Response logged.");
                    Debug.Log(listJson);
                    return;
                }

                Bind(guid, name, org.Id);
                EnableConnect();
                EditorPrefs.SetBool(PrefDone, true);
                Debug.Log("Flock Five: linked to Unity Cloud. Dashboard: https://cloud.unity.com/home/organizations/" + org.Id + "/projects/" + guid);
                Application.OpenURL("https://cloud.unity.com/home/organizations/" + org.Id + "/projects/" + guid);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Flock Five: cloud link failed — " + e.Message);
            }
        }

        static Type ConnectType()
        {
            return Type.GetType("UnityEditor.Connect.UnityConnect, UnityEditor.UnityConnectModule")
                ?? Type.GetType("UnityEditor.Connect.UnityConnect, UnityEditor");
        }

        static void Bind(string guid, string name, string orgId)
        {
            var ucType = ConnectType();
            var instProp = ucType != null
                ? ucType.GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            object inst = instProp != null ? instProp.GetValue(null) : null;
            if (inst != null)
            {
                var methods = inst.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name != "BindProject") continue;
                    var ps = methods[i].GetParameters();
                    try
                    {
                        if (ps.Length == 1)
                            methods[i].Invoke(inst, new object[] { guid });
                        else if (ps.Length == 3)
                            methods[i].Invoke(inst, new object[] { guid, name, orgId });
                        else if (ps.Length == 2)
                            methods[i].Invoke(inst, new object[] { guid, name });
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Flock Five: BindProject " + methods[i] + " — " + e.Message);
                    }
                }
            }

            var objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (objs == null || objs.Length == 0) return;
            var so = new SerializedObject(objs[0]);
            SetStr(so, "cloudProjectId", guid);
            SetStr(so, "projectName", name);
            SetStr(so, "organizationId", orgId);
            var en = so.FindProperty("cloudEnabled");
            if (en != null) en.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static void SetStr(SerializedObject so, string name, string v)
        {
            var p = so.FindProperty(name);
            if (p != null) p.stringValue = v;
        }

        static void EnableConnect()
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/UnityConnectSettings.asset");
            if (objs == null || objs.Length == 0) return;
            var so = new SerializedObject(objs[0]);
            var en = so.FindProperty("m_Enabled");
            if (en != null && !en.boolValue)
            {
                en.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        static void ShowLogin()
        {
            var ucType = Type.GetType("UnityEditor.Connect.UnityConnect, UnityEditor.UnityConnectModule");
            if (ucType == null)
                ucType = Type.GetType("UnityEditor.Connect.UnityConnect, UnityEditor");
            if (ucType == null) return;
            var instProp = ucType.GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object inst = instProp != null ? instProp.GetValue(null) : null;
            if (inst == null) return;
            var show = inst.GetType().GetMethod("ShowLogin", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (show != null) show.Invoke(inst, null);
        }

        struct Org { public string Id; public string Name; }

        static List<Org> ParseOrgs(string json)
        {
            var list = new List<Org>();
            if (string.IsNullOrEmpty(json)) return list;
            // Walk objects looking for id/name pairs.
            int i = 0;
            while (i < json.Length)
            {
                int idAt = json.IndexOf("\"id\"", i, StringComparison.Ordinal);
                if (idAt < 0) break;
                string id = JsonStringAfter(json, idAt);
                int nameAt = json.IndexOf("\"name\"", idAt, StringComparison.Ordinal);
                string name = nameAt > 0 && nameAt < idAt + 280 ? JsonStringAfter(json, nameAt) : "";
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name) &&
                    name != "id" && name.Length < 80)
                {
                    bool dup = false;
                    for (int k = 0; k < list.Count; k++)
                        if (list[k].Id == id) { dup = true; break; }
                    if (!dup) list.Add(new Org { Id = id, Name = name });
                }
                i = idAt + 4;
            }
            return list;
        }

        static string FindProjectId(string json, string name)
        {
            if (string.IsNullOrEmpty(json)) return "";
            int at = json.IndexOf("\"" + name + "\"", StringComparison.OrdinalIgnoreCase);
            if (at < 0) return "";
            // search nearby guid / id
            int window0 = Math.Max(0, at - 400);
            int window1 = Math.Min(json.Length, at + 400);
            string slice = json.Substring(window0, window1 - window0);
            return FindGuid(slice);
        }

        static string FindGuid(string json)
        {
            string g = FindString(json, "guid");
            if (!string.IsNullOrEmpty(g)) return g;
            g = FindString(json, "genesisId");
            if (!string.IsNullOrEmpty(g)) return g;
            return FindString(json, "id");
        }

        static string FindString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return "";
            int at = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (at < 0) return "";
            return JsonStringAfter(json, at);
        }

        static string JsonStringAfter(string json, int keyAt)
        {
            int colon = json.IndexOf(':', keyAt);
            if (colon < 0) return "";
            int q = json.IndexOf('"', colon + 1);
            if (q < 0) return "";
            int q2 = json.IndexOf('"', q + 1);
            if (q2 < 0) return "";
            return json.Substring(q + 1, q2 - q - 1);
        }

        static System.Threading.Tasks.Task<string> Get(string url, string token) =>
            Request(url, token, "GET", null);

        static System.Threading.Tasks.Task<string> Post(string url, string token, string json) =>
            Request(url, token, "POST", json);

        static System.Threading.Tasks.Task<string> Request(string url, string token, string method, string json)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<string>();
            var req = new UnityWebRequest(url, method);
            req.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(json))
            {
                byte[] raw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(raw);
                req.uploadHandler.contentType = "application/json";
            }
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");
            var op = req.SendWebRequest();
            op.completed += _ =>
            {
                string body = req.downloadHandler != null ? req.downloadHandler.text : "";
                if (req.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning("Flock Five cloud " + method + " " + url + " → " + req.responseCode + " " + body);
                tcs.TrySetResult(body);
                req.Dispose();
            };
            return tcs.Task;
        }
    }
}
#endif

