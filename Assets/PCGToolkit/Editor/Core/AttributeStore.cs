using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 属性的数据类型
    /// </summary>
    public enum AttribType
    {
        Float,
        Int,
        Vector2,
        Vector3,
        Vector4,
        Color,
        String
    }

    /// <summary>
    /// 属性所属的层级
    /// </summary>
    public enum AttribClass
    {
        Point,
        Vertex,
        Primitive,
        Detail
    }

    /// <summary>
    /// 单个属性的定义
    /// </summary>
    public class PCGAttribute
    {
        public string Name;
        public AttribType Type;
        public object DefaultValue;
        public List<object> Values = new List<object>();

        public PCGAttribute(string name, AttribType type, object defaultValue = null)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }

        public PCGAttribute Clone()
        {
            var clone = new PCGAttribute(Name, Type, DefaultValue);
            clone.Values = new List<object>(Values);
            return clone;
        }
    }

    /// <summary>
    /// 属性存储器，管理某一层级（Point/Vertex/Primitive/Detail）的所有属性。
    /// 对标 Houdini 的属性系统。
    /// </summary>
    public class AttributeStore
    {
        private Dictionary<string, PCGAttribute> _attributes = new Dictionary<string, PCGAttribute>();

        /// <summary>
        /// 创建一个新属性
        /// </summary>
        public PCGAttribute CreateAttribute(string name, AttribType type, object defaultValue = null)
        {
            var attr = new PCGAttribute(name, type, defaultValue);
            _attributes[name] = attr;
            return attr;
        }

        /// <summary>
        /// 获取属性，不存在则返回 null
        /// </summary>
        public PCGAttribute GetAttribute(string name)
        {
            _attributes.TryGetValue(name, out var attr);
            return attr;
        }

        /// <summary>
        /// 是否存在指定名称的属性
        /// </summary>
        public bool HasAttribute(string name)
        {
            return _attributes.ContainsKey(name);
        }

        /// <summary>
        /// 移除属性
        /// </summary>
        public bool RemoveAttribute(string name)
        {
            return _attributes.Remove(name);
        }

        /// <summary>
        /// 获取所有属性名
        /// </summary>
        public IEnumerable<string> GetAttributeNames()
        {
            return _attributes.Keys;
        }

        /// <summary>
        /// 获取所有属性
        /// </summary>
        public IEnumerable<PCGAttribute> GetAllAttributes()
        {
            return _attributes.Values;
        }

        /// <summary>
        /// 便捷方法：创建/更新属性并设置单个值，返回 this 以支持链式调用。
        /// </summary>
        public AttributeStore SetAttribute(string name, object value)
        {
            AttribType type = InferType(value);
            if (_attributes.TryGetValue(name, out var existing))
            {
                existing.DefaultValue = value;
                existing.Values.Clear();
                existing.Values.Add(value);
            }
            else
            {
                var attr = new PCGAttribute(name, type, value);
                attr.Values.Add(value);
                _attributes[name] = attr;
            }
            return this;
        }

        private static AttribType InferType(object value)
        {
            if (value is float || value is double) return AttribType.Float;
            if (value is int) return AttribType.Int;
            if (value is Vector2) return AttribType.Vector2;
            if (value is Vector3) return AttribType.Vector3;
            if (value is Vector4) return AttribType.Vector4;
            if (value is Color) return AttribType.Color;
            return AttribType.String;
        }

        /// <summary>
        /// 清空所有属性
        /// </summary>
        public void Clear()
        {
            _attributes.Clear();
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        public AttributeStore Clone()
        {
            var clone = new AttributeStore();
            foreach (var kvp in _attributes)
                clone._attributes[kvp.Key] = kvp.Value.Clone();
            return clone;
        }
    }
}
