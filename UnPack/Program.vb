Imports System
Imports System.Text
Imports System.IO
Imports System.IO.Compression


Module Program

    Private br As BinaryReader
    Private des As String
    Private source As String
    Private buffer As Byte()

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

            br = New BinaryReader(File.OpenRead(source))
            Dim sign As String = New String(br.ReadChars(2))
            MajorVersion = br.ReadByte
            MinorVersion = br.ReadByte
            Dim count As Int32

            Dim subfiles1 As List(Of FileDataVer1) = Nothing
            Dim subfiles2 As List(Of FileDataVer2) = Nothing

            If MajorVersion = 1 Then
                Dim entryHeaderOffset as Int16 = br.ReadInt16
                Dim entryHeaderCellSize as Int16 = br.ReadInt16
                count = br.ReadInt32
                subfiles1 = New List(Of FileDataVer1)()
            ElseIf MajorVersion = 2 Then
                Dim ECDSALength as Byte = br.ReadByte
                Dim ECDSA as Byte() = br.ReadBytes(ECDSALength)
                Dim ECDSAPadding as Byte() = br.ReadBytes(83 - ECDSALength)
                Dim filesChecksum as Int64 = br.ReadInt64
                Dim entryHeaderOffset as Int16 = br.ReadInt16
                Dim entryHeaderCellSize as Int16 = br.ReadInt16
                count = br.ReadInt32
                subfiles2 = New List(Of FileDataVer2)()
            ElseIf MajorVersion = 3 Then

            End If

            ' Console.WriteLine("Sign : {0} - MajorVersion : {1} - MinorVersion : {2} - Count : {3}", sign, MajorVersion, MinorVersion, count)


            For i As Int32 = 0 To count - 1
                If MajorVersion = 1 Then
                    subfiles1.Add(New FileDataVer1)
                ElseIf MajorVersion = 2 Then
                    subfiles2.Add(New FileDataVer2)
                End If
            Next

            While br.BaseStream.Position < br.BaseStream.Length

                Dim offset As Long = br.BaseStream.Position

                If MajorVersion = 1 Then


                ElseIf MajorVersion = 2 Then


                End If

            End While

        End If

        Console.ReadLine()
    End Sub

    Class FileDataVer1
        Public checksum As Int64 = br.ReadInt64
        Public offset As Int32 = br.ReadInt32
        Public size As Int32 = br.ReadInt32
        Public sizeUncompressed As Int32 = br.ReadInt32
        Public types As Int32 = br.ReadInt32 ' No : 0 , 1 : Gzip
    End Class

    Class FileDataVer2
        Public checksum As Int64 = br.ReadInt64
        Public offset As Int32 = br.ReadInt32
        Public sizeUncompressed As Int32 = br.ReadInt32
        Public size as Int32 = br.ReadInt32
        Public types As Byte = br.ReadByte ' No : 0 , 1 : Gzip
        Public unknow0 as Byte = br.ReadByte
        Public unknow1 as Byte = br.ReadByte
        Public unknow2 as Byte = br.ReadByte
        Public _sha256 As Int64 = br.ReadInt64
    End Class

End Module
