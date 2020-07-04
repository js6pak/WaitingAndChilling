using System.ComponentModel;
using UnityEngine;

namespace WaitingAndChilling
{
    public class Configuration
    {
        [Description("Position of lobby where players are teleported to")]
        public Vector3 Position { get; set; } = new Vector3(53, 1020, -43);
        
        [Description("Gives weapon kit to players")]
        public bool GiveItems { get; set; } = false;

        [Description("Role of players waiting in lobby")] // TODO include link to RoleType in SML api docs
        public RoleType Role { get; set; } = RoleType.Tutorial;
    }
}