Imports System
Imports System.Text
Imports System.IO
Imports System.IO.Compression
Imports ZstdNet

Module Program

    Private br As BinaryReader
    Private des As String
    Private source As String
    Private buffer As Byte()
    Private subfiles1 As List(Of FileDataVer1)
    Private subfiles2 As List(Of FileDataVer2)
    Private subfiles3 As List(Of FileDataVer3)

    Private ms As MemoryStream
    Private MajorVersion As Byte
    Private MinorVersion As Byte
    
    Sub Main(args As String())

        If args.Count = 0 Then
            Console.WriteLine("UnPack Tool - 2CongLc.vn")
        Else
            source = args(0)
        End If

        If File.Exists(source) Then

            des = Path.GetDirectoryName(source) & "\" & Path.GetFileNameWithoutExtension(source) & "\"
            br = New BinaryReader(File.OpenRead(source))

            Dim sign As String = New String(br.ReadChars(2))
            MajorVersion = br.ReadByte
            MinorVersion = br.ReadByte
            Dim count As Int32

            If MajorVersion = 1 Then
                Dim entryHeaderOffset As Int16 = br.ReadInt16
                Dim entryHeaderCellSize As Int16 = br.ReadInt16
                count = br.ReadInt32
                subfiles1 = New List(Of FileDataVer1)()
            ElseIf MajorVersion = 2 Then
                Dim ECDSALength As Byte = br.ReadByte
                Dim ECDSA As Byte() = br.ReadBytes(ECDSALength)
                Dim ECDSAPadding As Byte() = br.ReadBytes(83 - ECDSALength)
                Dim filesChecksum As Int64 = br.ReadInt64
                Dim entryHeaderOffset As Int16 = br.ReadInt16
                Dim entryHeaderCellSize As Int16 = br.ReadInt16
                count = br.ReadInt32
                subfiles2 = New List(Of FileDataVer2)()
            ElseIf MajorVersion = 3 Then
                Dim ECDSA As Byte() = br.ReadBytes(256)
                Dim filesChecksum As Int64 = br.ReadInt64
                count = br.ReadInt32
                subfiles3 = New List(Of FileDataVer3)()
            End If

            Console.Title = "Sign : " & sign & " | MajorVersion : " & MajorVersion & " | MinorVersion : " & MinorVersion & " | Count : " & count

            For i As Int32 = 0 To count - 1
                If MajorVersion = 1 Then
                    subfiles1.Add(New FileDataVer1)
                ElseIf MajorVersion = 2 Then
                    subfiles2.Add(New FileDataVer2)
                ElseIf MajorVersion = 3 Then
                    subfiles3.Add(New FileDataVer3)
                End If
            Next


            Directory.CreateDirectory(des)


            Dim name As String = Nothing

                If MajorVersion = 1 Then
                    For Each fd1 As FileDataVer1 In subfiles1

                        name = hexname(fd1.checksum)
                        Console.WriteLine("File Offset : {0} - File sizeUncompressed : {1} - File Size : {2} - File Name : {3}", fd1.offset, fd1.sizeUncompressed, fd1.size, name)

                        br.BaseStream.Position = fd1.offset

                        If fd1.types = 0 Then
                            buffer = br.ReadBytes(fd1.size)
                        Using bw As New BinaryWriter(File.Create(des + name))
                            bw.Write(buffer)
                        End Using
                    ElseIf fd1.types = 1 Then
                            ms = New MemoryStream()
                            buffer = br.ReadBytes(fd1.sizeUncompressed)
                        Dim fs As FileStream = File.Create(des + name)
                        Using dfs As New GZipStream(New MemoryStream(buffer), CompressionMode.Decompress)
                                dfs.CopyTo(fs)
                            End Using
                            fs.Close()
                        End If
                    Next
                ElseIf MajorVersion = 2 Then
                    For Each fd2 As FileDataVer2 In subfiles2

                        name = hexname(fd2.checksum)
                        Console.WriteLine("File Offset : {0} - File sizeUncompressed : {1} - File Size : {2} - File Name : {3}", fd2.offset, fd2.sizeUncompressed, fd2.size, name)

                        br.BaseStream.Position = fd2.offset

                        If fd2.types = 0 Then
                            buffer = br.ReadBytes(fd2.size)
                        Using bw As New BinaryWriter(File.Create(des + name))
                            bw.Write(buffer)
                        End Using
                    ElseIf fd2.types = 1 Then
                            ms = New MemoryStream()
                            buffer = br.ReadBytes(fd2.sizeUncompressed)
                        Dim fs As FileStream = File.Create(des + name)
                        Using dfs As New GZipStream(New MemoryStream(buffer), CompressionMode.Decompress)
                                dfs.CopyTo(fs)
                            End Using
                            fs.Close()
                        End If
                    Next
                ElseIf MajorVersion = 3 Then
                For Each fd3 As FileDataVer3 In subfiles3

                    name = hexname(fd3.checksum)
                    Console.WriteLine("File Offset : {0} - File sizeUncompressed : {1} - File Size : {2} - File Type : {3} - File Name : {4}", fd3.offset, fd3.sizeUncompressed, fd3.size, fd3.types, name)

                    br.BaseStream.Position = fd3.offset

                    If fd3.types = 0 Then
                        buffer = br.ReadBytes(fd3.size)
                        Using bw As New BinaryWriter(File.Create(des + name))
                            bw.Write(buffer)
                        End Using
                    ElseIf fd3.types = 3 Then

                        buffer = br.ReadBytes(fd3.sizeUncompressed)
                        ms = New MemoryStream(buffer)
                        ms.Position = 0
                        Dim fs As FileStream = File.Create(des + name)
                        Using zs As New ZstdNet.DecompressionStream(ms)
                            zs.CopyTo(fs)
                        End Using
                        fs.Close()


                        ' Tìm định dạng tệp tin
                        Dim br1 As New BinaryReader(File.OpenRead(des + name))
                        Dim magic As Byte() = br1.ReadBytes(4)
                        br1.Close()

                        If magic(0) = &HFF And magic(1) = &HD8 Then
                            File.Move(des + name, des + name + ".jpg")
                        ElseIf magic(0) = &H1A And magic(1) = &H45 And magic(2) = &HDF And magic(3) = &HA3 Then
                            File.Move(des + name, des + name + ".wbem")
                        ElseIf magic(0) = &H0 And magic(1) = &H1 And magic(2) = &H0 And magic(3) = &H0 Then
                            File.Move(des + name, des + name + ".ttf")
                        ElseIf magic(0) = &H44 And magic(1) = &H44 And magic(2) = &H53 Then
                            File.Move(des + name, des + name + ".dds")
                        Else


                            File.Move(des + name, des + name + ".unknow")

                        End If



                    End If

                Next

            End If


            br.Close()
            Console.WriteLine("UnPack Done !")
        End If

        br.Close()
        Console.ReadLine()
    End Sub

    Function hexname(hash As Byte()) As String
        Dim sb As New StringBuilder()
        sb.Length = 0
        For Each b As Byte In hash
          sb.Append(b.ToString("X2"))
        Next
        Return sb.ToString()
    End Function

    ' Cấu trúc dữ liệu block
    Class FileDataVer1
        Public checksum As Byte() = br.ReadBytes(8) ' Int64 = br.ReadInt64
        Public offset As Int32 = br.ReadInt32
        Public sizeUncompressed As Int32 = br.ReadInt32
        Public size As Int32 = br.ReadInt32
        Public types As Int32 = br.ReadInt32 ' No : 0 , 1 : Gzip
    End Class

    Class FileDataVer2
        Public checksum As Byte() = br.ReadBytes(8) 'Int64 = br.ReadInt64
        Public offset As Int32 = br.ReadInt32
        Public sizeUncompressed As Int32 = br.ReadInt32
        Public size As Int32 = br.ReadInt32
        Public types As Byte = br.ReadByte ' No : 0 , 1 : Gzip
        Public unknow0 as Byte = br.ReadByte
        Public unknow1 as Byte = br.ReadByte
        Public unknow2 as Byte = br.ReadByte
        Public _sha256 As Int64 = br.ReadInt64
    End Class

    Class FileDataVer3
        Public checksum As Byte() = br.ReadBytes(8) 'Int64 = br.ReadInt64
        Public offset As Int32 = br.ReadInt32
        Public sizeUncompressed As Int32 = br.ReadInt32
        Public size As Int32 = br.ReadInt32
        Public types As Byte = br.ReadByte ' No : 0 , 3 : zstd
        Public unknow0 as Byte = br.ReadByte
        Public unknow1 as Byte = br.ReadByte
        Public unknow2 as Byte = br.ReadByte
        Public _sha256 As Int64 = br.ReadInt64
    End Class

End Module
