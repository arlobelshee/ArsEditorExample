﻿using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace SimulatableApi
{
	internal class _FsDiskSimulated : IFsDisk
	{
		private readonly Dictionary<FSPath, _Node> _data = new Dictionary<FSPath, _Node>();

		public bool DirExists(FSPath path)
		{
			return _GetStorage(path).Kind == _StorageKind.Directory;
		}

		public bool FileExists(FSPath path)
		{
			return _GetStorage(path).Kind == _StorageKind.File;
		}

		public string TextContents(FSPath path)
		{
			var storage = _GetStorage(path);
			_ValidateStorage(path, storage);
		    return storage.TextContents;
		}

	    public byte[] RawContents(FSPath path)
	    {
            var storage = _GetStorage(path);
	        _ValidateStorage(path, storage);
            return storage.RawContents;
	    }

	    public void CreateDir(FSPath path)
		{
			while (true)
			{
				_data[path] = new _Node(_StorageKind.Directory);
				if (path.IsRoot)
					return;
				path = path.Parent;
			}
		}

		public void Overwrite(FSPath path, string newContents)
		{
			_data[path] = new _Node(_StorageKind.File) {
				TextContents = newContents
			};
		}

		public void DeleteDir(FSPath path)
		{
			if (_GetStorage(path).Kind == _StorageKind.File)
				throw new ArgumentException("path", string.Format("Path {0} was a file, and you attempted to delete a directory.", path.Absolute));
			_data.Remove(path);
		}

		public void DeleteFile(FSPath path)
		{
			if (_GetStorage(path).Kind == _StorageKind.Directory)
				throw new ArgumentException("path", string.Format("Path {0} was a directory, and you attempted to delete a file.", path.Absolute));
			_data.Remove(path);
		}

		public void MoveFile(FSPath src, FSPath dest)
		{
			if (_GetStorage(src).Kind != _StorageKind.File)
				throw new ArgumentException("path", string.Format("Attempted to move file {0}, which is not a file.", src.Absolute));
			if (_GetStorage(dest).Kind != _StorageKind.Missing)
				throw new ArgumentException("path", string.Format("Attempted to move file to destination {0}, which already exists.", dest.Absolute));
			_data[dest] = _data[src];
			_data.Remove(src);
		}

        private void _ValidateStorage(FSPath path, _Node storage)
        {
            if (storage.Kind == _StorageKind.Missing)
                throw new FileNotFoundException(string.Format("Could not find file '{0}'.", path.Absolute), path.Absolute);
            if (storage.Kind == _StorageKind.Directory)
                throw new UnauthorizedAccessException(string.Format("Access to the path '{0}' is denied.", path.Absolute));
        }

		[NotNull]
		private _Node _GetStorage([NotNull] FSPath path)
		{
			if (path.IsRoot)
				return new _Node(_StorageKind.Directory);
			_Node storage;
			return !_data.TryGetValue(path, out storage) ? new _Node(_StorageKind.Missing) : storage;
		}

		private class _Node
		{
			public _Node(_StorageKind storageKind)
			{
				Kind = storageKind;
				TextContents = string.Empty;
			    RawContents = new byte[0];
			}

			public _StorageKind Kind { get; private set; }
			public string TextContents { get; set; }
		    public byte[] RawContents { get; set; }
		}

		private enum _StorageKind
		{
			Directory,
			File,
			Missing,
		}
	}
}
