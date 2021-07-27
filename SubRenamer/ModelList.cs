using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace SubRenamer {
    public class ModelList {
        public ObservableCollection<Model> Models { get; } = new ObservableCollection<Model>();

        /// <summary>
        /// 添加原始视频
        /// </summary>
        /// <param name="files"></param>
        public void AddOriginalMovie(IEnumerable<string> files) {
            var fileList_comparer = files.OrderBy(n => n, new OrdinalComparer());

            var i = 0;
            foreach (var selectFileFileName in fileList_comparer) {
                if (i == Models.Count) {
                    Models.Add(new Model {
                        OriginalMovieFile = new FileInfo(selectFileFileName)
                    });
                } else {
                    Models[i].OriginalMovieFile = new FileInfo(selectFileFileName);
                }
                i++;
            }
            while (i < Models.Count) Models.RemoveAt(i);
        }

        /// <summary>
        /// 添加视频
        /// </summary>
        /// <param name="files"></param>
        public void AddMovie(IEnumerable<string> files) {
            var fileList_comparer = files.OrderBy(n => n, new OrdinalComparer());

            var i = 0;
            foreach (var selectFileFileName in fileList_comparer) {
                if (i == Models.Count) {
                    Models.Add(new Model {
                        MovieFile = new FileInfo(selectFileFileName)
                    });
                } else {
                    Models[i].MovieFile = new FileInfo(selectFileFileName);
                }
                i++;
            }
            while (i < Models.Count) Models.RemoveAt(i);
        }

        /// <summary>
        /// 添加字幕
        /// </summary>
        /// <param name="files"></param>
        public void AddSub(IEnumerable<string> files) {
            var fileList = files.Select(t => new {
                file = new FileInfo(t),
                name = new FileInfo(t).Name,
                nameOnly = Path.GetFileNameWithoutExtension(t)
            })
            //.OrderBy(t => t.file.FullName)
            .ToList();

            var i = -1;
            var lastSubNameOnly = "";

            var fileList_comparer = fileList.OrderBy(n => n.name, new OrdinalComparer());

            foreach (var selectFileFileName in fileList_comparer) {
                if (lastSubNameOnly != selectFileFileName.nameOnly) {
                    i++;
                    if (i < Models.Count) {
                        Models[i].SubFiles.Clear();
                    }
                }
                if (i == Models.Count) {
                    var model = new Model();
                    model.SubFiles.Add(selectFileFileName.file);
                    Models.Add(model);
                } else {
                    Models[i].SubFiles.Add(selectFileFileName.file);
                }
                lastSubNameOnly = selectFileFileName.nameOnly;
            }
            i++;
            while (i < Models.Count) Models.RemoveAt(i);
        }

        /// <summary>
        /// 拖动文件
        /// </summary>
        /// <param name="files"></param>
        /// <param name="eatSushi"></param>
        public void AddDropFiles(IEnumerable<string> files, bool eatSushi) {
            var originalMovieList = new List<string>();
            var movieList = new List<string>();
            var subList = new List<string>();

            foreach (var file in files) {
                var extension = new FileInfo(file).Extension.ToLower();
                switch (extension) {
                    case ".mp4":
                    case ".mkv":
                    case ".m2ts":
                    case ".avi":
                        if (!eatSushi || movieList.Count == 0) {
                            movieList.Add(file);
                        } else if (Utils.TestSimilarity(new FileInfo(movieList[0]).Name, new FileInfo(file).Name) > 30) {
                            movieList.Add(file);
                        } else {
                            originalMovieList.Add(file);
                        }
                        break;
                    case ".ass":
                    case ".ssa":
                    case ".srt":
                        subList.Add(file);
                        break;
                }
            }

            if (subList.Count > 0) AddSub(subList);
            if (eatSushi) {
                if (Models.Count > 0 && !string.IsNullOrWhiteSpace(Models[0].SubFileName)) {
                    if (movieList.Count > 0) {
                        if (Utils.TestSimilarity(Models[0].SubFiles[0].Name, new FileInfo(movieList[0]).Name) > 30) {
                            var tempList = movieList;
                            movieList = originalMovieList;
                            originalMovieList = tempList;
                        }
                    }
                }
            }
            if (originalMovieList.Count > 0) AddOriginalMovie(originalMovieList);
            if (movieList.Count > 0) AddMovie(movieList);
        }
    }
}
