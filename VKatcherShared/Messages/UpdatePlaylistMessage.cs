﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcherShared.Models;

namespace VKatcherShared.Messages
{
    [DataContract]
    public class UpdatePlaylistMessage
    {
        public UpdatePlaylistMessage(ObservableCollection<VKAudio> songs)
        {
            this.Songs = songs;
        }

        [DataMember]
        public ObservableCollection<VKAudio> Songs;
    }
}