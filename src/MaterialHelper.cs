using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class MaterialHelper
{
    private static Queue<Material> _materials;

    public static Material GetNextMaterial()
    {
        if (_materials == null)
        {
            var materials = new List<Material>();
            materials.Add(CreateMaterial("#800000".ToColor()));
            materials.Add(CreateMaterial("#8B0000".ToColor()));
            materials.Add(CreateMaterial("#A52A2A".ToColor()));
            materials.Add(CreateMaterial("#B22222".ToColor()));
            materials.Add(CreateMaterial("#DC143C".ToColor()));
            materials.Add(CreateMaterial("#FF0000".ToColor()));
            materials.Add(CreateMaterial("#FF6347".ToColor()));
            materials.Add(CreateMaterial("#FF7F50".ToColor()));
            materials.Add(CreateMaterial("#CD5C5C".ToColor()));
            materials.Add(CreateMaterial("#F08080".ToColor()));
            materials.Add(CreateMaterial("#E9967A".ToColor()));
            materials.Add(CreateMaterial("#FA8072".ToColor()));
            materials.Add(CreateMaterial("#FFA07A".ToColor()));
            materials.Add(CreateMaterial("#FF4500".ToColor()));
            materials.Add(CreateMaterial("#FF8C00".ToColor()));
            materials.Add(CreateMaterial("#FFA500".ToColor()));
            materials.Add(CreateMaterial("#FFD700".ToColor()));
            materials.Add(CreateMaterial("#B8860B".ToColor()));
            materials.Add(CreateMaterial("#DAA520".ToColor()));
            materials.Add(CreateMaterial("#EEE8AA".ToColor()));
            materials.Add(CreateMaterial("#BDB76B".ToColor()));
            materials.Add(CreateMaterial("#F0E68C".ToColor()));
            materials.Add(CreateMaterial("#808000".ToColor()));
            materials.Add(CreateMaterial("#FFFF00".ToColor()));
            materials.Add(CreateMaterial("#9ACD32".ToColor()));
            materials.Add(CreateMaterial("#556B2F".ToColor()));
            materials.Add(CreateMaterial("#6B8E23".ToColor()));
            materials.Add(CreateMaterial("#7CFC00".ToColor()));
            materials.Add(CreateMaterial("#7FFF00".ToColor()));
            materials.Add(CreateMaterial("#ADFF2F".ToColor()));
            materials.Add(CreateMaterial("#006400".ToColor()));
            materials.Add(CreateMaterial("#008000".ToColor()));
            materials.Add(CreateMaterial("#228B22".ToColor()));
            materials.Add(CreateMaterial("#00FF00".ToColor()));
            materials.Add(CreateMaterial("#32CD32".ToColor()));
            materials.Add(CreateMaterial("#90EE90".ToColor()));
            materials.Add(CreateMaterial("#98FB98".ToColor()));
            materials.Add(CreateMaterial("#8FBC8F".ToColor()));
            materials.Add(CreateMaterial("#00FA9A".ToColor()));
            materials.Add(CreateMaterial("#00FF7F".ToColor()));
            materials.Add(CreateMaterial("#2E8B57".ToColor()));
            materials.Add(CreateMaterial("#66CDAA".ToColor()));
            materials.Add(CreateMaterial("#3CB371".ToColor()));
            materials.Add(CreateMaterial("#20B2AA".ToColor()));
            materials.Add(CreateMaterial("#2F4F4F".ToColor()));
            materials.Add(CreateMaterial("#008080".ToColor()));
            materials.Add(CreateMaterial("#008B8B".ToColor()));
            materials.Add(CreateMaterial("#00FFFF".ToColor()));
            materials.Add(CreateMaterial("#00FFFF".ToColor()));
            materials.Add(CreateMaterial("#E0FFFF".ToColor()));
            materials.Add(CreateMaterial("#00CED1".ToColor()));
            materials.Add(CreateMaterial("#40E0D0".ToColor()));
            materials.Add(CreateMaterial("#48D1CC".ToColor()));
            materials.Add(CreateMaterial("#AFEEEE".ToColor()));
            materials.Add(CreateMaterial("#7FFFD4".ToColor()));
            materials.Add(CreateMaterial("#B0E0E6".ToColor()));
            materials.Add(CreateMaterial("#5F9EA0".ToColor()));
            materials.Add(CreateMaterial("#4682B4".ToColor()));
            materials.Add(CreateMaterial("#6495ED".ToColor()));
            materials.Add(CreateMaterial("#00BFFF".ToColor()));
            materials.Add(CreateMaterial("#1E90FF".ToColor()));
            materials.Add(CreateMaterial("#ADD8E6".ToColor()));
            materials.Add(CreateMaterial("#87CEEB".ToColor()));
            materials.Add(CreateMaterial("#87CEFA".ToColor()));
            materials.Add(CreateMaterial("#191970".ToColor()));
            materials.Add(CreateMaterial("#000080".ToColor()));
            materials.Add(CreateMaterial("#00008B".ToColor()));
            materials.Add(CreateMaterial("#0000CD".ToColor()));
            materials.Add(CreateMaterial("#0000FF".ToColor()));
            materials.Add(CreateMaterial("#4169E1".ToColor()));
            materials.Add(CreateMaterial("#8A2BE2".ToColor()));
            materials.Add(CreateMaterial("#4B0082".ToColor()));
            materials.Add(CreateMaterial("#483D8B".ToColor()));
            materials.Add(CreateMaterial("#6A5ACD".ToColor()));
            materials.Add(CreateMaterial("#7B68EE".ToColor()));
            materials.Add(CreateMaterial("#9370DB".ToColor()));
            materials.Add(CreateMaterial("#8B008B".ToColor()));
            materials.Add(CreateMaterial("#9400D3".ToColor()));
            materials.Add(CreateMaterial("#9932CC".ToColor()));
            materials.Add(CreateMaterial("#BA55D3".ToColor()));
            materials.Add(CreateMaterial("#800080".ToColor()));
            materials.Add(CreateMaterial("#D8BFD8".ToColor()));
            materials.Add(CreateMaterial("#DDA0DD".ToColor()));
            materials.Add(CreateMaterial("#EE82EE".ToColor()));
            materials.Add(CreateMaterial("#FF00FF".ToColor()));
            materials.Add(CreateMaterial("#DA70D6".ToColor()));
            materials.Add(CreateMaterial("#C71585".ToColor()));
            materials.Add(CreateMaterial("#DB7093".ToColor()));
            materials.Add(CreateMaterial("#FF1493".ToColor()));
            materials.Add(CreateMaterial("#FF69B4".ToColor()));
            materials.Add(CreateMaterial("#FFB6C1".ToColor()));
            materials.Add(CreateMaterial("#FFC0CB".ToColor()));
            materials.Add(CreateMaterial("#FAEBD7".ToColor()));
            materials.Add(CreateMaterial("#F5F5DC".ToColor()));
            materials.Add(CreateMaterial("#FFE4C4".ToColor()));
            materials.Add(CreateMaterial("#FFEBCD".ToColor()));
            materials.Add(CreateMaterial("#F5DEB3".ToColor()));
            materials.Add(CreateMaterial("#FFF8DC".ToColor()));
            materials.Add(CreateMaterial("#FFFACD".ToColor()));
            materials.Add(CreateMaterial("#FAFAD2".ToColor()));
            materials.Add(CreateMaterial("#FFFFE0".ToColor()));
            materials.Add(CreateMaterial("#8B4513".ToColor()));
            materials.Add(CreateMaterial("#A0522D".ToColor()));
            materials.Add(CreateMaterial("#D2691E".ToColor()));
            materials.Add(CreateMaterial("#CD853F".ToColor()));
            materials.Add(CreateMaterial("#F4A460".ToColor()));
            materials.Add(CreateMaterial("#DEB887".ToColor()));
            materials.Add(CreateMaterial("#D2B48C".ToColor()));
            materials.Add(CreateMaterial("#BC8F8F".ToColor()));
            materials.Add(CreateMaterial("#FFE4B5".ToColor()));
            materials.Add(CreateMaterial("#FFDEAD".ToColor()));
            materials.Add(CreateMaterial("#FFDAB9".ToColor()));
            materials.Add(CreateMaterial("#FFE4E1".ToColor()));
            materials.Add(CreateMaterial("#FFF0F5".ToColor()));

            _materials = new Queue<Material>(materials.OrderBy(x => Random.Range(-1, 2)));
        }

        Material current;
        _materials.Enqueue(current = _materials.Dequeue());
        return current;
    }

    private static Material CreateMaterial(Color color)
    {
        var material = new Material(Shader.Find("Battlehub/RTGizmos/Handles")) { color = color };
        material.SetFloat("_Offset", 1f);
        material.SetFloat("_MinAlpha", 1f);

        return material;
    }
}
