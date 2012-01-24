﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if MONOMAC
using MonoMac.OpenGL;
#else
using OpenTK.Graphics.ES20;
using ShaderType = OpenTK.Graphics.ES20.All;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    public class EffectPass
    {
        EffectTechnique _technique = null;
		GraphicsDevice _graphicsDevice;
		
		string name;
		int shaderProgram = 0;
		DXEffectObject.d3dx_state[] states;
		DXShader pixelShader;
		DXShader vertexShader;
		
		public EffectPass(EffectTechnique technique, DXEffectObject.d3dx_pass pass)
        {
            _technique = technique;
			_graphicsDevice = _technique._effect.GraphicsDevice;
			
			name = pass.name;
			states = pass.states;
			
			Console.WriteLine (technique.Name);
			
			shaderProgram = GL.CreateProgram ();
			
			// Set the parameters
			//is this nesesary, or just for VR?
			/*GL.ProgramParameter (shaderProgram,
				AssemblyProgramParameterArb.GeometryInputType,(int)All.Lines);
			GL.ProgramParameter (shaderProgram,
				AssemblyProgramParameterArb.GeometryOutputType, (int)All.Line);*/
			
			// Set the max vertices
			/*int maxVertices;
			GL.GetInteger (GetPName.MaxGeometryOutputVertices, out maxVertices);
			GL.ProgramParameter (shaderProgram,
				AssemblyProgramParameterArb.GeometryVerticesOut, maxVertices);*/
			
			bool needPixelShader = false;
			bool needVertexShader = false;
			foreach ( DXEffectObject.d3dx_state state in states) {
				if (state.operation.class_ == DXEffectObject.STATE_CLASS.PIXELSHADER) {
					needPixelShader = true;
					if (state.type == DXEffectObject.STATE_TYPE.CONSTANT) {
						pixelShader = (DXShader)state.parameter.data;
						GL.AttachShader (shaderProgram, pixelShader.shader);
					}
				} else if (state.operation.class_ == DXEffectObject.STATE_CLASS.VERTEXSHADER) {
					needVertexShader = true;
					if (state.type == DXEffectObject.STATE_TYPE.CONSTANT) {
						vertexShader = (DXShader)state.parameter.data;
						GL.AttachShader (shaderProgram, vertexShader.shader);
					}
				} else {
					throw new NotImplementedException();
				}
			}
			
			//If we have what we need, link now
			if ( (needPixelShader == (pixelShader != null)) &&
				 (needVertexShader == (vertexShader != null))) {
				GL.LinkProgram (shaderProgram);
			}
			
        }
		
		public void Apply ()
		{
			_technique._effect.OnApply();
			//Console.WriteLine (_technique._effect.Name+" - "+_technique.Name+" - "+Name);
			bool relink = false;
			foreach ( DXEffectObject.d3dx_state state in states) {
				
				//constants handled on init
				if (state.type == DXEffectObject.STATE_TYPE.CONSTANT) continue;
				
				if (state.operation.class_ == DXEffectObject.STATE_CLASS.PIXELSHADER ||
					state.operation.class_ == DXEffectObject.STATE_CLASS.VERTEXSHADER) {
					
					DXShader shader;
					switch (state.type) {
					case DXEffectObject.STATE_TYPE.EXPRESSIONINDEX:
						shader = (DXShader) (((DXExpression)state.parameter.data)
							.Evaluate (_technique._effect.Parameters));
						break;
					case DXEffectObject.STATE_TYPE.PARAMETER:
						//should be easy, but haven't seen it
					default:
						throw new NotImplementedException();
					}
					
					if (shader.shaderType == ShaderType.FragmentShader) {
						if (shader != pixelShader) {
							if (pixelShader != null) {
								GL.DetachShader (shaderProgram, pixelShader.shader);
							}
							relink = true;
							pixelShader = shader;
							GL.AttachShader (shaderProgram, pixelShader.shader);
						}
					} else if (shader.shaderType == ShaderType.VertexShader) {
						if (shader != vertexShader) {
							if (vertexShader != null) {
								GL.DetachShader(shaderProgram, vertexShader.shader);
							}
							relink = true;
							vertexShader = shader;
							GL.AttachShader (shaderProgram, vertexShader.shader);
						}
					}
					
				}
				
			}
			
			if (relink) {
#if !ES11
				//bind attributes. Default ones use in GL 1.1
				GL.BindAttribLocation(shaderProgram,
				                      GraphicsDevice.attributePosition,
				                      "g_Position");
				GL.BindAttribLocation(shaderProgram,
					                  GraphicsDevice.attributeTexCoord,
					                  "g_TexCoord");
				GL.BindAttribLocation(shaderProgram,
				                      GraphicsDevice.attributeColor,
				                      "g_Color");
				GL.BindAttribLocation(shaderProgram,
				                      GraphicsDevice.attributeNormal,
				                      "g_Normal");
#endif
				
				GL.LinkProgram (shaderProgram);
			}
			
			GL.UseProgram (shaderProgram);
			
			if (pixelShader != null) {
				pixelShader.Apply((uint)shaderProgram,
				                  _technique._effect.Parameters,
				                  _graphicsDevice);
			}
			if (vertexShader != null) {
				vertexShader.Apply((uint)shaderProgram,
				                  _technique._effect.Parameters,
				                  _graphicsDevice);
			}

		}
		
		public string Name { get { return name; } }
		
    }
}
