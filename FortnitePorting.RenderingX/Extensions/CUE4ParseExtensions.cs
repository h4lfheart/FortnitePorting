using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.Utils;

namespace FortnitePorting.RenderingX.Extensions;

public static class CUE4ParseExtensions
{
    extension<T>(T obj) where T : UObject
    {
        public void GatherTemplateProperties()
        {
            var current = obj;
            while (true)
            {
                current = current.Template?.Load<T>();
                if (current is null) break;

                foreach (var property in current.Properties)
                {
                    if (obj.Properties.Any(prop => prop.Name.Text.Equals(property.Name.Text))) continue;
                
                    obj.Properties.Add(property);
                }
            
                if (current.Template is null) break;
            }
        
            var fields = obj.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.DeclaringType == typeof(UObject)) continue;
            
                var targetProperty = obj.Properties.FirstOrDefault(prop => prop.Name.Text.Equals(field.Name));
                if (targetProperty is null) continue;
            
                field.SetValue(obj, targetProperty.Tag?.GetValue(field.FieldType));
            }
        }
    }

    extension(AbstractFileProvider provider)
    {
        public bool TryLoadObjectExports(string path, out IEnumerable<UObject> exports)
        {
            exports = [];
            try
            {
                exports = provider.LoadPackage(path).GetExports();
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            catch (AggregateException e) // wtf
            {
                return false;
            }

            return true;
        }
    }

    extension(UObject asset)
    {
        public bool TryLoadEditorData<T>(out T? editorData) where T : UObject
        {
            try
            {
                var provider = (AbstractFileProvider) asset.Owner!.Provider!;
                var path = asset.GetPathName().SubstringBeforeLast(".") + ".o.uasset";
                if (provider.TryLoadObjectExports(path, out var exports))
                {
                    editorData = exports.FirstOrDefault(export => export.GetType() == typeof(T)) as T;
                    return editorData is not null;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            editorData = null;
            return false;
        }
    }

    extension(IPropertyHolder propertyHolder)
    {
        public List<KeyValuePair<T, int>> GetAllProperties<T>(string name)
        {
            var propertyTags = new List<FPropertyTag>();
            foreach (var property in propertyHolder.Properties)
            {
                if (property.Name.Text.Equals(name))
                {
                    propertyTags.Add(property);
                }
            }

            var values = new List<KeyValuePair<T, int>>();
            foreach (var property in propertyTags)
            {
                var propertyValue = property.Tag.GetValue<T>();
                values.Add(new KeyValuePair<T, int>(propertyValue, property.ArrayIndex));
            }

            return values;
        }
    }
}