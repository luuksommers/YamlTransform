﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Yaml;
using YmlTransform.Models;

namespace YmlTransform
{
    public static class YmlTransformer
    {
        public static void TransformFile(string fileToTransform, string transformFile)
        {
            var transformation = GetTransformation(transformFile);
            TransformSingle(transformation, fileToTransform);
        }


        public static void TransformPath(string path, string transformFile, bool recursive)
        {
            var transformations = GetTransformation(transformFile);
            var files = Directory.GetFiles(path, "*.yml",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                TransformSingle(transformations, file);
            }
        }

        private static List<TransformItem> GetTransformation(string transformFile)
        {
            var transformation =
                Newtonsoft.Json.JsonConvert.DeserializeObject<List<TransformItem>>(File.ReadAllText(transformFile));
            return transformation;
        }

        private static void TransformSingle(List<TransformItem> transformations, string file)
        {
            using (var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var transformed = false;
                var formatter = new YamlSerializationFormatter(null, null);
                var item = new ProxyItem(formatter.ReadSerializedItem(fs, Path.GetFileName(file)));

                foreach (var transform in transformations)
                {
                    if (item.Path == transform.Path)
                    {
                        if (transform.Type == "Shared")
                        {
                            if (item.SharedFields.FirstOrDefault(a => a.FieldId == transform.FieldId) is ProxyFieldValue field)
                            {
                                Console.WriteLine(
                                    $"Updating file {file} section {transform.Type} id {transform.FieldId} to {transform.Value}");
                                field.Value = transform.Value;
                                transformed = true;
                            }
                        }
                        else if (transform.Type == "Languages")
                        {
                            var fields = item.Versions
                                .Where(a => transform.Languages == "*")
                                .SelectMany(a => a.Fields)
                                .Where(a => a.FieldId == transform.FieldId);

                            foreach (var itemFieldValue in fields)
                            {
                                var field = (ProxyFieldValue) itemFieldValue;
                                Console.WriteLine(
                                    $"Updating file {file} section {transform.Type} id {transform.FieldId} to {transform.Value}");
                                field.Value = transform.Value;
                                transformed = true;
                            }
                        }
                    }
                }


                if (transformed)
                {
                    Console.WriteLine($"Transformed: {file}");
                    fs.SetLength(0);
                    formatter.WriteSerializedItem(item, fs);
                }
            }
        }
    }
}
