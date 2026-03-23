using System;
using System.Collections.Generic;
using System.Linq;

namespace PCGToolkit.Core
{
    public static class PCGSelectionState
    {
        public static HashSet<int> SelectedPrimIndices = new HashSet<int>();
        public static HashSet<int> SelectedPointIndices = new HashSet<int>();
        public static HashSet<int> SelectedEdgeIndices = new HashSet<int>();
        public static PCGSelectMode CurrentMode = PCGSelectMode.Face;
        public static PCGGeometry SourceGeometry;
        public static int HoveredIndex = -1; // Currently hovered element index for preview

        public static event Action SelectionChanged;

        public static void AddToSelection(int index)
        {
            GetCurrentSet().Add(index);
            SelectionChanged?.Invoke();
        }

        public static void RemoveFromSelection(int index)
        {
            GetCurrentSet().Remove(index);
            SelectionChanged?.Invoke();
        }

        public static void ToggleSelection(int index)
        {
            var set = GetCurrentSet();
            if (!set.Remove(index))
                set.Add(index);
            SelectionChanged?.Invoke();
        }

        public static void Clear()
        {
            SelectedPrimIndices.Clear();
            SelectedPointIndices.Clear();
            SelectedEdgeIndices.Clear();
            SelectionChanged?.Invoke();
        }

        public static void SetMode(PCGSelectMode mode)
        {
            if (CurrentMode == mode) return;
            CurrentMode = mode;
            SelectionChanged?.Invoke();
        }

        public static int SelectionCount
        {
            get
            {
                switch (CurrentMode)
                {
                    case PCGSelectMode.Face: return SelectedPrimIndices.Count;
                    case PCGSelectMode.Edge: return SelectedEdgeIndices.Count;
                    case PCGSelectMode.Vertex: return SelectedPointIndices.Count;
                    default: return 0;
                }
            }
        }

        public static void NotifyChanged()
        {
            SelectionChanged?.Invoke();
        }

        private static HashSet<int> GetCurrentSet()
        {
            switch (CurrentMode)
            {
                case PCGSelectMode.Face: return SelectedPrimIndices;
                case PCGSelectMode.Edge: return SelectedEdgeIndices;
                case PCGSelectMode.Vertex: return SelectedPointIndices;
                default: return SelectedPrimIndices;
            }
        }

        // ---- Serialization ----

        [Serializable]
        public class SerializedSelection
        {
            public int[] PrimIndices;
            public int[] PointIndices;
            public int[] EdgeIndices;
            public int Mode;
        }

        public static string SerializeToJson()
        {
            var data = new SerializedSelection
            {
                PrimIndices = SelectedPrimIndices.ToArray(),
                PointIndices = SelectedPointIndices.ToArray(),
                EdgeIndices = SelectedEdgeIndices.ToArray(),
                Mode = (int)CurrentMode
            };
            return UnityEngine.JsonUtility.ToJson(data);
        }

        public static void RestoreFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var data = UnityEngine.JsonUtility.FromJson<SerializedSelection>(json);
                if (data == null) return;

                SelectedPrimIndices = new HashSet<int>(data.PrimIndices ?? new int[0]);
                SelectedPointIndices = new HashSet<int>(data.PointIndices ?? new int[0]);
                SelectedEdgeIndices = new HashSet<int>(data.EdgeIndices ?? new int[0]);
                CurrentMode = (PCGSelectMode)data.Mode;
                SelectionChanged?.Invoke();
            }
            catch
            {
                // Ignore deserialization errors
            }
        }
    }
}
