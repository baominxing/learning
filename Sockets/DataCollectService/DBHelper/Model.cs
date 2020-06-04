﻿using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MDCSingleDLL.DBHelper
{
    #region Mongo数据库表对应实体
    public class BaseEntity
    {
        public long MachineId { get; set; }
        public string CreationTime { get; set; }
    }

    public class Machine : BaseEntity
    {
        public Machine()
        {
            Map = new Map();
            State = new CurrentState();
        }
        public int TenantId { get; set; }
        public string MachineCode { get; set; }
        public byte Uid { get; set; }
        public string Password { get; set; }
        public string ProgramName { get; set; }
        public Map Map { get; set; }
        public CurrentState State { get; set; }
    }
    public class Map : BaseEntity
    {
        public Map()
        {
            MapArray = new List<MapArray>();
        }
        public List<MapArray> MapArray { get; set; }
    }
    public class MapArray
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
    public class Alarm
    {
        public Alarm()
        {
            AlarmArray = new List<AlarmArray>();
        }
        public List<AlarmArray> AlarmArray { get; set; }

    }
    public class AlarmArray : BaseEntity
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
    public class State
    {
        public State()
        {
            StateArray = new List<StateArray>();
        }
        public List<StateArray> StateArray { get; set; }
    }
    public class StateArray : BaseEntity
    {
        public string Code { get; set; }
    }
    public class Capacity
    {
        public Capacity()
        {
            CapacityArray = new List<CapacityArray>();
        }
        public List<CapacityArray> CapacityArray { get; set; }
    }
    public class CapacityArray : BaseEntity
    {
        public int Count { get; set; }
        public int Yield { get; set; }
        public int OrginalCount { get; set; }
        public string ProgramName { get; set; }
        public int CurrentProgramCount { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class Parameter
    {
        public Parameter()
        {
            ParameterArray = new List<ParameterArray>();
        }
        public List<ParameterArray> ParameterArray { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class ParameterArray : BaseEntity
    {
    }
    /// <summary>
    /// 状态对象
    /// </summary>
    public class CurrentState
    {
        /// <summary>
        /// 上一次的报警编号
        /// </summary>
        public string LastAlarmCode { get; set; }
        /// <summary>
        /// 上一次的报警内容
        /// </summary>
        public string LastAlarmMessage { get; set; }
        /// <summary>
        /// 上次Count值
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 上一次的计数器数值
        /// </summary>
        public int LastCount { get; set; }
        /// <summary>
        /// 上一次程序号
        /// </summary>
        public string LastProgramName { get; set; }
    }
    #endregion

    #region 数据项持久化对象
    public class DataItem
    {
        public ushort Id { get; set; }
        public byte Type { get; set; }
        public byte[] Value { get; set; }
    }
    #endregion

}
