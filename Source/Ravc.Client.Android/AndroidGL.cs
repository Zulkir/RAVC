﻿#region License
/*
Copyright (c) 2014 RAVC Project - Daniil Rodin

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System;
using System.Text;
using ObjectGL;
using OpenTK.Graphics.ES30;

namespace Ravc.Client.Android
{
    public unsafe class AndroidGL : IGL
    {
        public void ActiveTexture(int texture)
        {
            GL.ActiveTexture((All)texture);
        }

        public void AttachShader(uint program, uint shader)
        {
            GL.AttachShader(program, shader);
        }

        public void BeginTransformFeedback(int primitiveMode)
        {
            GL.BeginTransformFeedback((All)primitiveMode);
        }

        public void BindAttribLocation(uint program, uint index, string name)
        {
            GL.BindAttribLocation(program, index, name);
        }

        public void BindBuffer(int target, uint buffer)
        {
            GL.BindBuffer((All)target, buffer);
        }

        public void BindBufferBase(int target, uint index, uint buffer)
        {
            GL.BindBufferBase((All)target, index, buffer);
        }

        public void BindFramebuffer(int target, uint framebuffer)
        {
            GL.BindFramebuffer((All)target, framebuffer);
        }

        public void BindRenderbuffer(int target, uint renderbuffer)
        {
            GL.BindRenderbuffer((All)target, renderbuffer);
        }

        public void BindSampler(uint unit, uint sampler)
        {
            GL.BindSampler(unit, sampler);
        }

        public void BindTexture(int target, uint texture)
        {
            GL.BindTexture((All)target, texture);
        }

        public void BindTransformFeedback(int target, uint id)
        {
            GL.BindTransformFeedback((All)target, id);
        }

        public void BindVertexArray(uint array)
        {
            GL.BindVertexArray(array);
        }

        public void BlendColor(float red, float green, float blue, float alpha)
        {
            GL.BlendColor(red, green, blue, alpha);
        }

        public void BlendEquation(int mode)
        {
            GL.BlendEquation((All)mode);
        }

        public void BlendEquation(uint buf, int mode)
        {
            //GL.BlendEquation(buf, (All)mode);
            throw new NotSupportedException();
        }

        public void BlendEquationSeparate(int modeRGB, int modeAlpha)
        {
            GL.BlendEquationSeparate((All)modeRGB, (All)modeAlpha);
        }

        public void BlendEquationSeparate(uint buf, int modeRGB, int modeAlpha)
        {
            //GL.BlendEquationSeparate(buf, (All)modeRGB, (All)modeAlpha);
            throw new NotSupportedException();
        }

        public void BlendFunc(int sfactor, int dfactor)
        {
            GL.BlendFunc((All)sfactor, (All)dfactor);
        }

        public void BlendFunc(uint buf, int sfactor, int dfactor)
        {
            //GL.BlendFunc(buf, (All)sfactor, (All)dfactor);
            throw new NotSupportedException();
        }

        public void BlendFuncSeparate(int srcRGB, int dstRGB, int srcAlpha, int dstAlpha)
        {
            GL.BlendFuncSeparate((All)srcRGB, (All)dstRGB, (All)srcAlpha, (All)dstAlpha);
        }

        public void BlendFuncSeparate(uint buf, int srcRGB, int dstRGB, int srcAlpha, int dstAlpha)
        {
            //GL.BlendFuncSeparate(buf, (All)srcRGB, (All)dstRGB, (All)srcAlpha, (All)dstAlpha);
            throw new NotSupportedException();
        }

        public void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, int filter)
        {
            GL.BlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, (All)filter);
        }

        public void BufferData(int target, IntPtr size, IntPtr data, int usage)
        {
            GL.BufferData((All)target, size, data, (All)usage);
        }

        public void BufferSubData(int target, IntPtr offset, IntPtr size, IntPtr data)
        {
            GL.BufferSubData((All)target, offset, size, data);
        }

        public void ClearBuffer(int buffer, int drawBuffer, int* value)
        {
            GL.ClearBuffer((All)buffer, drawBuffer, value);
        }

        public void ClearBuffer(int buffer, int drawBuffer, uint* value)
        {
            GL.ClearBuffer((All)buffer, drawBuffer, value);
        }

        public void ClearBuffer(int buffer, int drawBuffer, float* value)
        {
            GL.ClearBuffer((All)buffer, drawBuffer, value);
        }

        public void ClearBuffer(int buffer, int drawBuffer, float depth, int stencil)
        {
            GL.ClearBuffer((All)buffer, drawBuffer, depth, stencil);
        }

        public void CompileShader(uint shader)
        {
            GL.CompileShader(shader);
        }

        public void CompressedTexImage1D(int target, int level, int internalformat, int width, int border, int imageSize, IntPtr data)
        {
            //GL.CompressedTexImage1D((All)target, level, (All)internalformat, width, border, imageSize, data);
            throw new NotSupportedException();
        }

        public void CompressedTexImage2D(int target, int level, int internalformat, int width, int height, int border, int imageSize, IntPtr data)
        {
            GL.CompressedTexImage2D((All)target, level, (All)internalformat, width, height, border, imageSize, data);
        }

        public void CompressedTexImage3D(int target, int level, int internalFormat, int width, int height, int depth, int border, int imageSize, IntPtr data)
        {
            GL.CompressedTexImage3D((All)target, level, (All)internalFormat, width, height, depth, border, imageSize, data);
        }

        public void CompressedTexSubImage1D(int target, int level, int xoffset, int width, int format, int imageSize, IntPtr data)
        {
            //GL.CompressedTexSubImage1D((All)target, level, xoffset, width, (All)format, imageSize, data);
            throw new NotSupportedException();
        }

        public void CompressedTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, IntPtr data)
        {
            GL.CompressedTexSubImage2D((All)target, level, xoffset, yoffset, width, height, (All)format, imageSize, data);
        }

        public void CompressedTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, IntPtr data)
        {
            GL.CompressedTexSubImage3D((All)target, level, xoffset, yoffset, zoffset, width, height, depth, (All)format, imageSize, data);
        }

        public uint CreateProgram()
        {
            return (uint)GL.CreateProgram();
        }

        public uint CreateShader(int shaderType)
        {
            return (uint)GL.CreateShader((All)shaderType);
        }

        public void CullFace(int mode)
        {
            GL.CullFace((All)mode);
        }

        public void DeleteBuffers(int n, uint* buffers)
        {
            GL.DeleteBuffers(n, buffers);
        }

        public void DeleteFramebuffers(int n, uint* framebuffers)
        {
            GL.DeleteFramebuffers(n, framebuffers);
        }

        public void DeleteProgram(uint program)
        {
            GL.DeleteProgram(program);
        }

        public void DeleteRenderbuffers(int n, uint* renderbuffers)
        {
            GL.DeleteRenderbuffers(n, renderbuffers);
        }

        public void DeleteSamplers(int n, uint* samplers)
        {
            GL.DeleteSamplers(n, samplers);
        }

        public void DeleteShader(uint shader)
        {
            GL.DeleteShader(shader);
        }

        public void DeleteTextures(int n, uint* textures)
        {
            GL.DeleteTextures(n, textures);
        }

        public void DeleteTransformFeedbacks(int n, uint* ids)
        {
            //GL.DeleteTransformFeedbacks(n, ids);
            throw new NotSupportedException();
        }

        public void DeleteVertexArrays(int n, uint* arrays)
        {
            GL.DeleteVertexArrays(n, arrays);
        }

        public void DepthFunc(int func)
        {
            GL.DepthFunc((All)func);
        }

        public void DepthMask(bool flag)
        {
            GL.DepthMask(flag);
        }

        public void DepthRange(float nearVal, float farVal)
        {
            GL.DepthRange(nearVal, farVal);
        }

        public void DepthRangeIndexed(uint index, double nearVal, double farVal)
        {
            //GL.DepthRangeIndexed(index, nearVal, farVal);
            throw new NotSupportedException();
        }

        public void Disable(int cap)
        {
            GL.Disable((All)cap);
        }

        public void DisableVertexAttribArray(uint index)
        {
            GL.DisableVertexAttribArray(index);
        }

        public void DrawArrays(int mode, int first, int count)
        {
            GL.DrawArrays((All)mode, first, count);
        }

        public void DrawArraysIndirect(int mode, IntPtr indirect)
        {
            //GL.DrawArraysIndirect((All)mode, indirect);
            throw new NotSupportedException();
        }

        public void DrawArraysInstanced(int mode, int first, int count, int primcount)
        {
            GL.DrawArraysInstanced((All)mode, first, count, primcount);
        }

        public void DrawArraysInstancedBaseInstance(int mode, int first, int count, int primcount, uint baseinstance)
        {
            //GL.DrawArraysInstancedBaseInstance((All)mode, first, count, primcount, baseinstance);
            throw new NotSupportedException();
        }

        public void DrawElements(int mode, int count, int type, IntPtr indices)
        {
            GL.DrawElements((All)mode, count, (All)type, indices);
        }

        public void DrawElementsBaseVertex(int mode, int count, int type, IntPtr indices, int basevertex)
        {
            //GL.DrawElementsBaseVertex((All)mode, count, (DrawElementsType)type, indices, basevertex);
            throw new NotSupportedException();
        }

        public void DrawElementsIndirect(int mode, int type, IntPtr indirect)
        {
            //GL.DrawElementsIndirect((All)mode, (All)type, indirect);
            throw new NotSupportedException();
        }

        public void DrawElementsInstanced(int mode, int count, int type, IntPtr indices, int primcount)
        {
            GL.DrawElementsInstanced((All)mode, count, (All)type, indices, primcount);
        }

        public void DrawElementsInstancedBaseInstance(int mode, int count, int type, IntPtr indices, int primcount, uint baseinstance)
        {
            //GL.DrawElementsInstancedBaseInstance((All)mode, count, (All)type, indices, primcount, baseinstance);
            throw new NotSupportedException();
        }

        public void DrawElementsInstancedBaseVertex(int mode, int count, int type, IntPtr indices, int primcount, int basevertex)
        {
            //GL.DrawElementsInstancedBaseVertex((All)mode, count, (All)type, indices, primcount, basevertex);
            throw new NotSupportedException();
        }

        public void DrawElementsInstancedBaseVertexBaseInstance(int mode, int count, int type, IntPtr indices, int primcount, int basevertex, uint baseinstance)
        {
            //GL.DrawElementsInstancedBaseVertexBaseInstance((All)mode, count, (All)type, indices, primcount, basevertex, baseinstance);
            throw new NotSupportedException();
        }

        public void DrawRangeElements(int mode, uint start, uint end, int count, int type, IntPtr indices)
        {
            GL.DrawRangeElements((All)mode, start, end, count, (All)type, indices);
        }

        public void DrawRangeElementsBaseVertex(int mode, uint start, uint end, int count, int type, IntPtr indices, int basevertex)
        {
            //GL.DrawRangeElementsBaseVertex((All)mode, start, end, count, (All)type, indices, basevertex);
            throw new NotSupportedException();
        }

        public void DrawTransformFeedback(int mode, uint id)
        {
            //GL.DrawTransformFeedback((All)mode, id);
            throw new NotSupportedException();
        }

        public void DrawTransformFeedbackInstanced(int mode, uint id, int primcount)
        {
            //GL.DrawTransformFeedbackInstanced((All)mode, id, primcount);
            throw new NotSupportedException();
        }

        public void DrawTransformFeedbackStream(int mode, uint id, uint stream)
        {
            //GL.DrawTransformFeedbackStream((All)mode, id, stream);
            throw new NotSupportedException();
        }

        public void DrawTransformFeedbackStreamInstanced(int mode, uint id, uint stream, int primcount)
        {
            //GL.DrawTransformFeedbackStreamInstanced((All)mode, id, stream, primcount);
            throw new NotSupportedException();
        }

        public void Enable(int cap)
        {
            GL.Enable((All)cap);
        }

        public void EnableVertexAttribArray(uint index)
        {
            GL.EnableVertexAttribArray(index);
        }

        public void EndTransformFeedback()
        {
            GL.EndTransformFeedback();
        }

        public void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer)
        {
            GL.FramebufferRenderbuffer((All)target, (All)attachment, (All)renderbuffertarget, renderbuffer);
        }

        public void FramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level)
        {
            GL.FramebufferTexture2D((All)target, (All)attachment, (All)textarget, texture, level);
        }

        public void FramebufferTextureLayer(int target, int attachment, uint texture, int level, int layer)
        {
            GL.FramebufferTextureLayer((All)target, (All)attachment, texture, level, layer);
        }

        public void FrontFace(int mode)
        {
            GL.FrontFace((All)mode);
        }

        public void GenBuffers(int n, uint* buffers)
        {
            GL.GenBuffers(n, buffers);
        }

        public void GenerateMipmap(int target)
        {
            GL.GenerateMipmap((All)target);
        }

        public void GenFramebuffers(int n, uint* framebuffers)
        {
            GL.GenFramebuffers(n, framebuffers);
        }

        public void GenRenderbuffers(int n, uint* renderbuffers)
        {
            GL.GenRenderbuffers(n, renderbuffers);
        }

        public void GenSamplers(int n, uint* samplers)
        {
            GL.GenSamplers(n, samplers);
        }

        public void GenTextures(int n, uint* textures)
        {
            GL.GenTextures(n, textures);
        }

        public void GenTransformFeedbacks(int n, uint* ids)
        {
            //GL.GenTransformFeedbacks(n, ids);
            throw new NotSupportedException();
        }

        public void GenVertexArrays(int n, uint* arrays)
        {
            GL.GenVertexArrays(n, arrays);
        }

        public int GetAttribLocation(uint program, string name)
        {
            return GL.GetAttribLocation(program, new StringBuilder(name));
        }

        public void GetFloat(int pname, float* data)
        {
            GL.GetFloat((All)pname, data);
        }

        public void GetInteger(int pname, int* data)
        {
            GL.GetInteger((All)pname, data);
        }

        public void GetProgram(uint program, int pname, int* parameters)
        {
            GL.GetProgram(program, (All)pname, parameters);
        }

        public string GetProgramInfoLog(uint program)
        {
            var builder = new StringBuilder(1 << 16);
            int length;
            GL.GetProgramInfoLog(program, 1 << 16, out length, builder);
            return builder.ToString(0, length);
        }

        public void GetShader(uint shader, int pname, int* parameters)
        {
            GL.GetShader(shader, (All)pname, parameters);
        }

        public string GetShaderInfoLog(uint shader)
        {
            var builder = new StringBuilder(1 << 16);
            int length;
            GL.GetShaderInfoLog(shader, 1 << 16, out length, builder);
            return builder.ToString(0, length);
        }

        public string GetString(int name)
        {
            return GL.GetString((All)name);
        }

        public uint GetUniformBlockIndex(uint program, string uniformBlockName)
        {
            return (uint)GL.GetUniformBlockIndex(program, new StringBuilder(uniformBlockName));
        }

        public int GetUniformLocation(uint program, string name)
        {
            return GL.GetUniformLocation(program, new StringBuilder(name));
        }

        public void LinkProgram(uint program)
        {
            GL.LinkProgram(program);
        }

        public IntPtr MapBufferRange(int target, IntPtr offset, IntPtr length, int access)
        {
            return GL.MapBufferRange((All)target, offset, length, access);
        }

        public void PatchParameter(int pname, int value)
        {
            //GL.PatchParameter((All)pname, value);
            throw new NotSupportedException();
        }

        public void PixelStore(int pname, int param)
        {
            GL.PixelStore((All)pname, param);
        }

        public void PolygonMode(int face, int mode)
        {
            //GL.PolygonMode((All)face, (All)mode);
            //throw new NotSupportedException();
        }

        public void RenderbufferStorage(int target, int internalformat, int width, int height)
        {
            GL.RenderbufferStorage((All)target, (All)internalformat, width, height);
        }

        public void RenderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height)
        {
            GL.RenderbufferStorageMultisample((All)target, samples, (All)internalformat, width, height);
        }

        public void SampleMask(uint maskNumber, uint mask)
        {
            //GL.SampleMask(maskNumber, mask);
            //throw new NotSupportedException();
        }

        public void SamplerParameter(uint sampler, int pname, float param)
        {
            GL.SamplerParameter(sampler, (All)pname, param);
        }

        public void SamplerParameter(uint sampler, int pname, int param)
        {
            GL.SamplerParameter(sampler, (All)pname, param);
        }

        public void SamplerParameter(uint sampler, int pname, float* parameters)
        {
            GL.SamplerParameter(sampler, (All)pname, parameters);
        }

        public void Scissor(int x, int y, int width, int height)
        {
            GL.Scissor(x, y, width, height);
        }

        public void ScissorIndexed(uint index, int left, int bottom, int width, int height)
        {
            //GL.ScissorIndexed(index, left, bottom, width, height);
            throw new NotSupportedException();
        }

        public void ShaderSource(uint shader, string strings)
        {
            GL.ShaderSource(shader, 1, new[] {strings}, new[] {strings.Length});
        }

        public void StencilFuncSeparate(int face, int func, int reference, uint mask)
        {
            GL.StencilFuncSeparate((All)face, (All)func, reference, mask);
        }

        public void StencilMaskSeparate(int face, uint mask)
        {
            GL.StencilMaskSeparate((All)face, mask);
        }

        public void StencilOpSeparate(int face, int sfail, int dpfail, int dppass)
        {
            GL.StencilOpSeparate((All)face, (All)sfail, (All)dpfail, (All)dppass);
        }

        public void TexImage1D(int target, int level, int internalFormat, int width, int border, int format, int type, IntPtr data)
        {
            //GL.TexImage1D((All)target, level, (All)internalFormat, width, border, (All)format, (All)type, data);
            throw new NotSupportedException();
        }

        public void TexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr data)
        {
            GL.TexImage2D((All)target, level, internalFormat, width, height, border, (All)format, (All)type, data);
        }

        public void TexImage3D(int target, int level, int internalFormat, int width, int height, int depth, int border, int format, int type, IntPtr data)
        {
            GL.TexImage3D((All)target, level, internalFormat, width, height, depth, border, (All)format, (All)type, data);
        }

        public void TexParameter(int target, int pname, float param)
        {
            GL.TexParameter((All)target, (All)pname, param);
        }

        public void TexParameter(int target, int pname, int param)
        {
            GL.TexParameter((All)target, (All)pname, param);
        }

        public void TexStorage1D(int target, int levels, int internalformat, int width)
        {
            //GL.TexStorage1D((All)target, levels, internalformat, width);
            throw new NotSupportedException();
        }

        public void TexStorage2D(int target, int levels, int internalformat, int width, int height)
        {
            GL.TexStorage2D((All)target, levels, (All)internalformat, width, height);
        }

        public void TexStorage2DMultisample(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations)
        {
            //GL.TexStorage2DMultisample((All)target, samples, (All)internalformat, width, height, fixedsamplelocations);
            throw new NotSupportedException();
        }

        public void TexStorage3D(int target, int levels, int internalformat, int width, int height, int depth)
        {
            GL.TexStorage3D((All)target, levels, (All)internalformat, width, height, depth);
        }

        public void TexStorage3DMultisample(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations)
        {
            //GL.TexStorage3DMultisample((All)target, samples, (All)internalformat, width, height, depth, fixedsamplelocations);
            throw new NotSupportedException();
        }

        public void TexSubImage1D(int target, int level, int xoffset, int width, int format, int type, IntPtr data)
        {
            //GL.TexSubImage1D((All)target, level, xoffset, width, (All)format, (All)type, data);
            throw new NotSupportedException();
        }

        public void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr data)
        {
            GL.TexSubImage2D((All)target, level, xoffset, yoffset, width, height, (All)format, (All)type, data);
        }

        public void TexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, IntPtr data)
        {
            GL.TexSubImage3D((All)target, level, xoffset, yoffset, zoffset, width, height, depth, (All)format, (All)type, data);
        }

        public void TransformFeedbackVaryings(uint program, int count, string[] varyings, int bufferMode)
        {
            if (varyings.Length != 1)
                throw new NotSupportedException();
            GL.TransformFeedbackVaryings(program, count, varyings[0], (All)bufferMode);
        }

        public void Uniform(int location, int v0)
        {
            GL.Uniform1(location, v0);
        }

        public void UniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding)
        {
            GL.UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
        }

        public bool UnmapBuffer(int target)
        {
            return GL.UnmapBuffer((All)target);
        }

        public void UseProgram(uint program)
        {
            GL.UseProgram(program);
        }

        public void VertexAttribPointer(uint index, int size, int type, bool normalized, int stride, IntPtr pointer)
        {
            GL.VertexAttribPointer(index, size, (All)type, normalized, stride, pointer);
        }

        public void VertexAttribIPointer(uint index, int size, int type, int stride, IntPtr pointer)
        {
            GL.VertexAttribIPointer(index, size, (All)type, stride, pointer);
        }

        public void VertexAttribDivisor(uint index, uint divisor)
        {
            GL.VertexAttribDivisor(index, divisor);
        }

        public void Viewport(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
        }

        public void ViewportIndexed(uint index, float x, float y, float w, float h)
        {
            //GL.ViewportIndexed(index, x, y, w, h);
            throw new NotSupportedException();
        }
    }
}